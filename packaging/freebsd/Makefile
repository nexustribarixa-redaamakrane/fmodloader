# Port skeleton for FreeBSD (drop into /usr/ports/deskutils/fmodloader or use with poudriere)
PORTNAME=	fmodloader
DISTVERSION=	1.0.65
CATEGORIES=	deskutils graphics
MASTER_SITES=	https://github.com/nexustribarixa-redaamakrane/fmodloader/releases/download/BETA-v${DISTVERSION}-beta/
DISTNAME=	fModLoader-${DISTVERSION}-freebsd-x64

MAINTAINER=	nexustribarixa@users.noreply.github.com
COMMENT=	Dynamic font glyph modification utility for .modcompat fonts
LICENSE=	GPLv3+

ONLY_FOR_ARCHS=	amd64
USES=		tar:xz
NO_BUILD=	yes

PLIST_FILES=	bin/fModLoader \
		bin/fModLoader_CLI

do-install:
	${INSTALL_PROGRAM} ${WRKSRC}/fModLoader ${STAGEDIR}${PREFIX}/bin/
	${INSTALL_PROGRAM} ${WRKSRC}/fModLoader_CLI ${STAGEDIR}${PREFIX}/bin/

.include <bsd.port.mk>
