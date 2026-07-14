using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using fModLoader.Models;

namespace fModLoader.Services;

public class SvgPathParser
{
    public static List<GlyphContour> Parse(string pathD)
    {
        var contours = new List<GlyphContour>();
        if (string.IsNullOrWhiteSpace(pathD))
            return contours;

        // Tokenize path data
        var tokens = Tokenize(pathD);
        int i = 0;

        GlyphContour? currentContour = null;
        double currentX = 0;
        double currentY = 0;
        double startX = 0;
        double startY = 0;

        while (i < tokens.Count)
        {
            string cmd = tokens[i];
            if (IsCommand(cmd))
            {
                i++;
                bool isRelative = char.IsLower(cmd[0]);
                char type = char.ToUpper(cmd[0]);

                switch (type)
                {
                    case 'M':
                        {
                            if (currentContour != null && currentContour.Nodes.Count > 0)
                            {
                                contours.Add(currentContour);
                            }
                            currentContour = new GlyphContour();

                            double x = ParseDouble(tokens[i++]);
                            double y = ParseDouble(tokens[i++]);
                            if (isRelative)
                            {
                                x += currentX;
                                y += currentY;
                            }
                            currentX = x;
                            currentY = y;
                            startX = x;
                            startY = y;

                            currentContour.Nodes.Add(new PathNode(x, y));

                            // Subsequent pairs of coordinates are treated as implicit Lineto commands
                            while (i < tokens.Count && !IsCommand(tokens[i]))
                            {
                                double lx = ParseDouble(tokens[i++]);
                                double ly = ParseDouble(tokens[i++]);
                                if (isRelative)
                                {
                                    lx += currentX;
                                    ly += currentY;
                                }
                                currentX = lx;
                                currentY = ly;
                                currentContour.Nodes.Add(new PathNode(lx, ly));
                            }
                        }
                        break;

                    case 'L':
                        {
                            if (currentContour == null) currentContour = new GlyphContour();
                            while (i < tokens.Count && !IsCommand(tokens[i]))
                            {
                                double x = ParseDouble(tokens[i++]);
                                double y = ParseDouble(tokens[i++]);
                                if (isRelative)
                                {
                                    x += currentX;
                                    y += currentY;
                                }
                                currentX = x;
                                currentY = y;
                                currentContour.Nodes.Add(new PathNode(x, y));
                            }
                        }
                        break;

                    case 'H':
                        {
                            if (currentContour == null) currentContour = new GlyphContour();
                            while (i < tokens.Count && !IsCommand(tokens[i]))
                            {
                                double x = ParseDouble(tokens[i++]);
                                if (isRelative) x += currentX;
                                currentX = x;
                                currentContour.Nodes.Add(new PathNode(x, currentY));
                            }
                        }
                        break;

                    case 'V':
                        {
                            if (currentContour == null) currentContour = new GlyphContour();
                            while (i < tokens.Count && !IsCommand(tokens[i]))
                            {
                                double y = ParseDouble(tokens[i++]);
                                if (isRelative) y += currentY;
                                currentY = y;
                                currentContour.Nodes.Add(new PathNode(currentX, y));
                            }
                        }
                        break;

                    case 'C':
                        {
                            if (currentContour == null) currentContour = new GlyphContour();
                            while (i < tokens.Count && !IsCommand(tokens[i]))
                            {
                                double x1 = ParseDouble(tokens[i++]);
                                double y1 = ParseDouble(tokens[i++]);
                                double x2 = ParseDouble(tokens[i++]);
                                double y2 = ParseDouble(tokens[i++]);
                                double x = ParseDouble(tokens[i++]);
                                double y = ParseDouble(tokens[i++]);

                                if (isRelative)
                                {
                                    x1 += currentX; y1 += currentY;
                                    x2 += currentX; y2 += currentY;
                                    x += currentX; y += currentY;
                                }

                                // In our PathNode model, we assign CpOut to the previous node, and CpIn to the current node
                                if (currentContour.Nodes.Count > 0)
                                {
                                    var prevNode = currentContour.Nodes[^1];
                                    prevNode.CpOut = Tuple.Create(x1, y1);
                                }

                                var newNode = new PathNode(x, y)
                                {
                                    CpIn = Tuple.Create(x2, y2)
                                };
                                currentContour.Nodes.Add(newNode);

                                currentX = x;
                                currentY = y;
                            }
                        }
                        break;

                    case 'S':
                        {
                            if (currentContour == null) currentContour = new GlyphContour();
                            while (i < tokens.Count && !IsCommand(tokens[i]))
                            {
                                double x2 = ParseDouble(tokens[i++]);
                                double y2 = ParseDouble(tokens[i++]);
                                double x = ParseDouble(tokens[i++]);
                                double y = ParseDouble(tokens[i++]);

                                if (isRelative)
                                {
                                    x2 += currentX; y2 += currentY;
                                    x += currentX; y += currentY;
                                }

                                // Determine reflection of previous cpOut for cpIn equivalent
                                double x1 = currentX;
                                double y1 = currentY;
                                if (currentContour.Nodes.Count > 0)
                                {
                                    var prevNode = currentContour.Nodes[^1];
                                    if (prevNode.CpOut != null)
                                    {
                                        x1 = 2 * currentX - prevNode.CpOut.Item1;
                                        y1 = 2 * currentY - prevNode.CpOut.Item2;
                                    }
                                }

                                if (currentContour.Nodes.Count > 0)
                                {
                                    currentContour.Nodes[^1].CpOut = Tuple.Create(x1, y1);
                                }

                                var newNode = new PathNode(x, y)
                                {
                                    CpIn = Tuple.Create(x2, y2)
                                };
                                currentContour.Nodes.Add(newNode);

                                currentX = x;
                                currentY = y;
                            }
                        }
                        break;

                    case 'Z':
                        {
                            if (currentContour != null)
                            {
                                currentContour.Closed = true;
                                currentX = startX;
                                currentY = startY;
                                contours.Add(currentContour);
                                currentContour = null;
                            }
                        }
                        break;
                }
            }
            else
            {
                i++; // Skip invalid tokens
            }
        }

        if (currentContour != null && currentContour.Nodes.Count > 0)
        {
            contours.Add(currentContour);
        }

        return contours;
    }

    private static List<string> Tokenize(string pathD)
    {
        var tokens = new List<string>();
        var matches = Regex.Matches(pathD, @"([a-df-zzA-DF-ZZ])|(-?\d*\.?\d+(?:[eE][-+]?\d+)?)");
        foreach (Match match in matches)
        {
            tokens.Add(match.Value);
        }
        return tokens;
    }

    private static bool IsCommand(string token)
    {
        if (token.Length != 1) return false;
        char c = token[0];
        return char.IsLetter(c);
    }

    private static double ParseDouble(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0.0;
    }
}
