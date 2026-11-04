import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent))

from ois import OperativeIdentity, OISError, tokenize, validate_callsign, validate_display_name, validate_nickname


def test_defaults():
    identity = OperativeIdentity()
    assert identity.display_name == "Tinashe Mukasa"
    assert identity.callsign == "SHADOW-07"
    assert identity.nickname == ""
    assert "SHADOW-07" in "\n".join(identity.dossier_lines)


def test_callsign_validation():
    assert validate_callsign("nightfalcon") == "NIGHTFALCON"
    assert validate_callsign("sable 7") == "SABLE 7"
    with pytest.raises(OISError):
        validate_callsign("DROP TABLE;")
    with pytest.raises(OISError):
        validate_callsign("THIS CALLSIGN IS WAY TOO LONG TO EVER FIT ANY HUD")


def test_display_name_validation():
    assert validate_display_name("Tendai Makoni") == "Tendai Makoni"
    with pytest.raises(OISError):
        validate_display_name("<script>alert(1)</script>")
    with pytest.raises(OISError):
        validate_display_name("x" * 29)


def test_zero_width_and_homoglyphs_are_normalised():
    assert validate_callsign("SAB\u200bLE\u200b") == "SABLE"
    assert validate_display_name("Tendai\u200b Makoni") == "Tendai Makoni"


def test_blocked_terms_are_rejected():
    with pytest.raises(OISError):
        OperativeIdentity(display_name="adolf hitler")


def test_tokenize():
    identity = OperativeIdentity(display_name="Tendai Makoni", callsign="SABLE", nickname="T")
    assert tokenize("{OIS_CALLSIGN} went live", identity) == "SABLE went live"
    assert tokenize("{OIS_DISPLAY} file", identity) == "Tendai Makoni file"
    assert tokenize("{OIS_NICKNAME} says no", identity) == "T says no"


def test_empty_fields_fall_back_to_defaults():
    identity = OperativeIdentity(display_name="", callsign="", nickname="")
    assert identity.display_name == "Tinashe Mukasa"
    assert identity.callsign == "SHADOW-07"
