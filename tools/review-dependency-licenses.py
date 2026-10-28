"""Rebuild the exact restored NuGet licence inventory without recording machine paths."""
import json
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
packages = {}
for assets in sorted(root.glob("*/obj/project.assets.json")):
    data = json.loads(assets.read_text(encoding="utf-8-sig"))
    folders = [Path(folder) for folder in data["packageFolders"]]
    for identity, library in data["libraries"].items():
        if library.get("type") != "package":
            continue
        name, version = identity.split("/", 1)
        key = (name, version)
        if key not in packages:
            package_dir = next(folder / library["path"] for folder in folders if (folder / library["path"]).is_dir())
            spec = ET.parse(next(package_dir.glob("*.nuspec"))).getroot()
            nodes = {element.tag.rsplit("}", 1)[-1]: element for element in spec.iter()}
            license_node = nodes.get("license")
            url_node = nodes.get("licenseUrl")
            value = license_node.text if license_node is not None else None
            kind = license_node.get("type") if license_node is not None else "legacyUrl"
            opening = None
            if kind == "file" and value:
                opening = (package_dir / value).read_text(encoding="utf-8-sig").strip()[:180]
            packages[key] = dict(package=name, version=version, license=value,
                                 licenseType=kind, licenseUrl=url_node.text if url_node is not None else None,
                                 licenseFileOpening=opening, projects=[])
        packages[key]["projects"].append(assets.parent.parent.name)
output = dict(reviewDate="2026-10-28", packages=[packages[key] for key in sorted(packages, key=lambda key: (key[0].lower(), key[1]))])
(root / "docs/dependency-license-inventory.json").write_text(json.dumps(output, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
for item in output["packages"]:
    print(item["package"], item["version"], item["license"] or item["licenseFileOpening"] or item["licenseUrl"])
