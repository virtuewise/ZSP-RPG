"""ZSP Operative Identity System (OIS).

Fixed canon protagonist (Tinashe Mukasa) + player-chosen display name,
operational callsign and optional nickname. No determinism impact.
"""

from __future__ import annotations

import re
import unicodedata
from dataclasses import dataclass, field

DEFAULT_DISPLAY = "Tinashe Mukasa"
DEFAULT_CALLSIGN = "SHADOW-07"
DEFAULT_NICKNAME = ""

# Minimal illustrative block-list. Production builds must ship a maintained
# vendor list covering the target locales, plus a server-side policy check.
BLOCKED_TERMS = (
    "faggot", "nigger", "nigga", "kike", "tranny", "spic", "chink",
    "retard", "hitler",
)

ZERO_WIDTH = "".join(chr(c) for c in range(0x200B, 0x2010)) + "\ufeff"
_CALL_RE = re.compile(r"^[A-Z0-9][A-Z0-9\- ]{0,11}$")


class OISError(ValueError):
    """Raised when an identity field fails validation."""


def _clean(text: str) -> str:
    if not isinstance(text, str):
        return ""
    text = "".join(ch for ch in text if ch not in ZERO_WIDTH)
    text = unicodedata.normalize("NFKC", text)
    return re.sub(r"\s+", " ", text).strip()


def _check_blocked(value: str) -> None:
    lowered = value.lower().replace(" ", "").replace("-", "")
    for term in BLOCKED_TERMS:
        if term in lowered:
            raise OISError("that name is not available")


def _letters_only(value: str) -> bool:
    for ch in value:
        if ch.isalpha() or ch in " '-.":
            continue
        return False
    return True


def validate_display_name(value: str) -> str:
    value = _clean(value)
    if not value:
        return DEFAULT_DISPLAY
    if len(value) > 28:
        raise OISError("display name: 28 characters max")
    if not _letters_only(value):
        raise OISError("display name: letters, spaces, apostrophes, hyphens and periods only")
    _check_blocked(value)
    return value


def validate_callsign(value: str) -> str:
    value = _clean(value).upper()
    if not value:
        return DEFAULT_CALLSIGN
    if not _CALL_RE.match(value):
        raise OISError("callsign: A-Z, 0-9, hyphen and space only; 12 characters max")
    _check_blocked(value)
    return value


def validate_nickname(value: str) -> str:
    value = _clean(value)
    if not value:
        return DEFAULT_NICKNAME
    if len(value) > 14:
        raise OISError("nickname: 14 characters max")
    if not all(ch.isalpha() or ch.isdigit() or ch in " '\u2019-." for ch in value):
        raise OISError("nickname: letters, numbers, spaces, apostrophes and hyphens only")
    _check_blocked(value)
    return value


def tokenize(text: str, identity: "OperativeIdentity") -> str:
    if not text:
        return text
    return (
        text.replace("{OIS_DISPLAY}", identity.display_name)
        .replace("{OIS_CALLSIGN}", identity.callsign)
        .replace("{OIS_NICKNAME}", identity.nickname or identity.callsign)
        .replace("{OIS_FULL}", identity.display_name)
    )


@dataclass
class OperativeIdentity:
    display_name: str = DEFAULT_DISPLAY
    callsign: str = DEFAULT_CALLSIGN
    nickname: str = DEFAULT_NICKNAME
    campaign: str = "The Black Ledger"

    def __post_init__(self):
        self.display_name = validate_display_name(self.display_name)
        self.callsign = validate_callsign(self.callsign)
        self.nickname = validate_nickname(self.nickname)

    @property
    def dossier_lines(self) -> list[str]:
        return [
            "PLAYER OPERATIVE PROFILE",
            f"  callsign       {self.callsign}",
            f"  display name   {self.display_name}",
            f"  campaign       {self.campaign}",
            "  protagonist    Tinashe Mukasa",
            "  status         ACTIVE THREAT",
        ]

    def to_dict(self) -> dict:
        return {
            "displayName": self.display_name,
            "callsign": self.callsign,
            "nickname": self.nickname,
            "campaign": self.campaign,
        }

    @classmethod
    def from_dict(cls, data) -> "OperativeIdentity":
        if not data:
            return cls()
        return cls(
            display_name=data.get("displayName", DEFAULT_DISPLAY),
            callsign=data.get("callsign", DEFAULT_CALLSIGN),
            nickname=data.get("nickname", DEFAULT_NICKNAME),
            campaign=data.get("campaign", "The Black Ledger"),
        )
