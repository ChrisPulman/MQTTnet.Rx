#!/usr/bin/env python3
"""Validate the complete MQTTnet.Rx NuGet package family."""

from __future__ import annotations

import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree


EXPECTED_PACKAGE_IDS = {
    "MQTTnet.Rx.ABPlc",
    "MQTTnet.Rx.ABPlc.Reactive",
    "MQTTnet.Rx.AspNetCore",
    "MQTTnet.Rx.AspNetCore.Reactive",
    "MQTTnet.Rx.Client",
    "MQTTnet.Rx.Client.Reactive",
    "MQTTnet.Rx.Mitsubishi",
    "MQTTnet.Rx.Mitsubishi.Reactive",
    "MQTTnet.Rx.Modbus",
    "MQTTnet.Rx.Modbus.Reactive",
    "MQTTnet.Rx.OmronPlc",
    "MQTTnet.Rx.OmronPlc.Reactive",
    "MQTTnet.Rx.S7Plc",
    "MQTTnet.Rx.S7Plc.Reactive",
    "MQTTnet.Rx.Server",
    "MQTTnet.Rx.Server.Reactive",
    "MQTTnet.Rx.SerialPort",
    "MQTTnet.SerialPort.Reactive",
    "MQTTnet.Rx.TwinCAT",
    "MQTTnet.TwinCATRx.Reactive",
}


def package_id(package_path: Path) -> str:
    """Read the package ID from a NuGet package's nuspec."""
    with zipfile.ZipFile(package_path) as archive:
        nuspec_names = [name for name in archive.namelist() if name.endswith(".nuspec")]
        if len(nuspec_names) != 1:
            raise ValueError(
                f"{package_path.name} contains {len(nuspec_names)} nuspec files; expected one"
            )

        root = ElementTree.fromstring(archive.read(nuspec_names[0]))
        metadata = next((element for element in root.iter() if element.tag.endswith("metadata")), None)
        if metadata is None:
            raise ValueError(f"{package_path.name} has no nuspec metadata element")

        identifier = next((element for element in metadata if element.tag.endswith("id")), None)
        if identifier is None or not identifier.text:
            raise ValueError(f"{package_path.name} has no package ID")

        return identifier.text


def main() -> int:
    """Validate package identities and return an operating-system exit code."""
    if len(sys.argv) != 2:
        print("usage: verify-package-layout.py <package-directory>", file=sys.stderr)
        return 2

    package_directory = Path(sys.argv[1]).resolve()
    packages = sorted(
        path
        for path in package_directory.glob("*.nupkg")
        if not path.name.endswith(".symbols.nupkg")
    )
    if not packages:
        print(f"no NuGet packages found in {package_directory}", file=sys.stderr)
        return 1

    discovered: dict[str, Path] = {}
    errors: list[str] = []
    for package in packages:
        try:
            identifier = package_id(package)
        except (OSError, ValueError, zipfile.BadZipFile, ElementTree.ParseError) as error:
            errors.append(str(error))
            continue

        if identifier in discovered:
            errors.append(
                f"duplicate package ID {identifier}: {discovered[identifier].name}, {package.name}"
            )
        discovered[identifier] = package

    missing = EXPECTED_PACKAGE_IDS - discovered.keys()
    unexpected = discovered.keys() - EXPECTED_PACKAGE_IDS
    errors.extend(f"missing package ID: {identifier}" for identifier in sorted(missing))
    errors.extend(f"unexpected package ID: {identifier}" for identifier in sorted(unexpected))

    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1

    for identifier in sorted(discovered):
        print(f"{identifier}: {discovered[identifier].name}")
    print(f"verified {len(discovered)} MQTTnet.Rx packages")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
