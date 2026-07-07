using CommunityToolkit.Mvvm.ComponentModel;

namespace FModLoaderInstaller.ViewModels;

public partial class LicensePageViewModel : WizardPageBase
{
    [ObservableProperty] private bool _isAccepted;

    public LicensePageViewModel()
    {
        PageTitle = "License Agreement";
        PageSubtitle = "GNU General Public License v3.0";
        CanGoNext = false;
    }

    partial void OnIsAcceptedChanged(bool value)
    {
        CanGoNext = value;
    }

    public string LicenseText => @"GNU General Public License version 3.0
(GNU GPL v3.0)

                    fModLoader

This General Public License is meant to guarantee your freedom to share and
change all versions of a program — to make sure it remains free software for
all its users.

When we speak of free software, we are referring to freedom, not price.
This General Public License is designed to make sure that you have the
freedom to distribute copies of free software, and charge for this service
if you wish, that you receive source code or can get it if you want it, that
you can change the software or use pieces of it in new free programs, and
that you know you can do these things.

                TERMS AND CONDITIONS

  0. Definitions.

  ""This License"" refers to version 3 of the GNU General Public License.

  ""The Program"" refers to any copyrightable work licensed under this
License. Each licensee is addressed as ""you"". ""Licensees"" and ""recipients""
may be individuals or organizations.

  ""Modify"" a work means to copy from or adapt all or part of the work in
a fashion requiring copyright permission, other than the making of an exact
copy. The resulting work is called a ""modified version"" of the earlier work
or a work ""based on"" the earlier work.

  ""Source code"" for a work means the preferred form of the work for making
modifications to it. ""Object code"" means any non-source form of a work.

  1. Source Code.

  The ""source code"" for a work means the preferred form of the work for
making modifications to it.

  2. Basic Permissions.

  All rights granted under this License are granted for the term of
copyright on the Program, and are irrevocable provided the stated
conditions are met. This License explicitly affirms your unlimited
permission to run the unmodified Program.

  3. Protecting Users' Legal Rights From Anti-Circumvention Law.

  No covered work shall be deemed part of an effective technological
measure under any applicable law fulfilling obligations under article 11
of the WIPO copyright treaty adopted on 20 December 1996.

  4. Conveying Verbatim Copies.

  You may convey verbatim copies of the Program's source code as you
receive it, in any medium, provided that you conspicuously and
appropriately publish on each copy an appropriate copyright notice;
keep intact all notices stating that this License and any non-permissive
terms added in accord with section 7 apply to the code.

  5. Conveying Modified Source Versions.

  You may convey a work based on the Program, or the modifications to
produce it from the Program, in the form of source code under the terms
of section 4, provided that you also meet all of these conditions.

  6. Conveying Non-Source Forms.

  You may convey a work in object code form under the terms of sections 4
and 5, provided that you also convey the machine-readable Corresponding
Source under the terms of this License.

  7. Additional Terms.

  ""Additional permissions"" are terms that supplement the terms of this
License by making exceptions from one or more of its conditions.

  15. Disclaimer of Warranty.

  THERE IS NO WARRANTY FOR THE PROGRAM, TO THE EXTENT PERMITTED BY
APPLICABLE LAW. EXCEPT WHEN OTHERWISE STATED IN WRITING THE COPYRIGHT
HOLDERS AND/OR OTHER PARTIES PROVIDE THE PROGRAM ""AS IS"" WITHOUT WARRANTY
OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO,
THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
PURPOSE. THE ENTIRE RISK AS TO THE QUALITY AND PERFORMANCE OF THE PROGRAM
IS WITH YOU. SHOULD THE PROGRAM PROVE DEFECTIVE, YOU ASSUME THE COST OF
ALL NECESSARY SERVICING, REPAIR OR CORRECTION.

  16. Limitation of Liability.

  IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING
WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MODIFIES AND/OR CONVEYS
THE PROGRAM AS PERMITTED ABOVE, BE LIABLE TO YOU FOR DAMAGES, INCLUDING
ANY GENERAL, SPECIAL, INCIDENTAL OR CONSEQUENTIAL DAMAGES ARISING OUT OF
THE USE OR INABILITY TO USE THE PROGRAM (INCLUDING BUT NOT LIMITED TO LOSS
OF DATA OR DATA BEING RENDERED INACCURATE OR LOSSES SUSTAINED BY YOU OR
THIRD PARTIES OR A FAILURE OF THE PROGRAM TO OPERATE WITH ANY OTHER
PROGRAMS), EVEN IF SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGES.

                   END OF TERMS AND CONDITIONS

Copyright (C) 2024-2026 Nexus Tribarixa
fModLoader is licensed under the GNU General Public License v3.0
https://github.com/nexustribarixa-redaamakrane/fmodloader";
}
