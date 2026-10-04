"""Build Unity-ready PNG textures from the local source material.

Usage:
    py build_textures.py --source-dds ../../rice --generated-atlas path/to/atlas.png
"""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


STAGE_IDS = ("01", "02", "03", "04", "06")


def convert_originals(source_root: Path, output_root: Path) -> None:
    output_root.mkdir(parents=True, exist_ok=True)
    for source in source_root.rglob("*.dds"):
        relative = source.relative_to(source_root).with_suffix(".png")
        destination = output_root / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        with Image.open(source) as image:
            image.convert("RGBA").save(destination, optimize=True)


def build_stage_textures(atlas_path: Path, output_root: Path) -> None:
    output_root.mkdir(parents=True, exist_ok=True)
    with Image.open(atlas_path) as source:
        atlas = source.convert("RGBA")

    atlas.save(output_root / "Rice_GeneratedAtlas.png", optimize=True)
    width, height = atlas.size

    for index, stage_id in enumerate(STAGE_IDS):
        left = round(index * width / len(STAGE_IDS))
        right = round((index + 1) * width / len(STAGE_IDS))
        cell = atlas.crop((left, 0, right, height))
        alpha_box = cell.getchannel("A").getbbox()
        if alpha_box is None:
            raise RuntimeError(f"Generated atlas cell {index} has no visible pixels")

        plant = cell.crop(alpha_box)
        target = Image.new("RGBA", (512, 1024), (0, 0, 0, 0))
        max_width, max_height = 464, 976
        scale = min(max_width / plant.width, max_height / plant.height)
        resized = plant.resize(
            (max(1, round(plant.width * scale)), max(1, round(plant.height * scale))),
            Image.Resampling.LANCZOS,
        )
        x = (target.width - resized.width) // 2
        y = target.height - resized.height - 24
        target.alpha_composite(resized, (x, y))
        target.save(output_root / f"Rice_Stage{stage_id}.png", optimize=True)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dds", type=Path, required=True)
    parser.add_argument("--generated-atlas", type=Path, required=True)
    parser.add_argument("--package-root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()

    textures = args.package_root / "Runtime" / "Textures"
    convert_originals(args.source_dds.resolve(), textures / "Original")
    build_stage_textures(args.generated_atlas.resolve(), textures / "Stages")


if __name__ == "__main__":
    main()
