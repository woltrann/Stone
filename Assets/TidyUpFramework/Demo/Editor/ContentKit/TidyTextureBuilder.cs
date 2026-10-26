using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static partial class TidyTextureBuilder
    {
        private const int BoardsPerPlank = 2;
        private const int PlankGrainCellsAcross = 3;
        private const int PlankGrainCellsAlong = 2;
        private const int PlankFibreCellsAcross = 24;
        private const int PlankFibreCellsAlong = 6;
        private const int GrainOffsetRange = 97;
        private const float PlankRingFrequency = 2.5f;
        private const float PlankRingWarp = 3f;
        private const float PlankGapFraction = 0.0022f;
        private const float PlankBevelFraction = 0.008f;
        private const float PlankBevelDarkening = 0.78f;
        private const float PlankGapDarkness = 0.35f;
        private const float PlankMinTint = 0.86f;
        private const float PlankMaxTint = 1.1f;
        private const float PlankHueJitter = 0.02f;
        private const float KnotChance = 0.45f;
        private const float KnotRadiusFraction = 0.09f;
        private const float KnotStretch = 0.35f;
        private const int BoardShiftSalt = 31;
        private const int BoardTintSalt = 17;
        private const int KnotSalt = 53;

        private const int GrainRingCount = 10;
        private const float GrainRingWarp = 3f;
        private const int GrainFibreCellsAcross = 96;
        private const int GrainFibreCellsAlong = 6;

        private const float StoneAspect = 0.75f;
        private const float StoneJitter = 0.5f;
        private const float StoneMortarFraction = 0.006f;
        private const float StoneCornerFraction = 0.25f;
        private const float StoneEdgeNoiseFraction = 0.012f;
        private const int StoneEdgeNoiseCells = 16;
        private const int StoneSurfaceCells = 8;
        private const int StoneCrackCells = 6;
        private const float StoneEdgeShadeFraction = 0.02f;

        private const float TileGroutMaximum = 0.3f;
        private const float TileBevelWidth = 0.06f;
        private const float TileGroutDarkness = 0.55f;

        public static Texture2D WoodPlanks(int size, Color light, Color dark, int planksAcross, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            int planks = Mathf.Max(1, planksAcross);
            canvas.Fill((u, v) => PlankPixel(u, v, canvas.Width, planks, light, dark, seed));
            return canvas.ToTexture("WoodPlanks");
        }

        public static Texture2D WoodGrain(int size, Color light, Color dark, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            canvas.Fill((u, v) => GrainPixel(u, v, light, dark, seed));
            return canvas.ToTexture("WoodGrain");
        }

        public static Texture2D StoneWall(int size, Color mortar, Color stone, int rows, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            int rowCount = Mathf.Max(1, rows);
            int blocks = Mathf.Max(2, Mathf.RoundToInt(rowCount * StoneAspect));
            canvas.Fill((u, v) => StonePixel(u, v, canvas.Width, rowCount, blocks, mortar, stone, seed));
            return canvas.ToTexture("StoneWall");
        }

        public static Texture2D FloorTiles(int size, Color a, Color b, int tiles, float groutWidth, int seed)
        {
            var canvas = new TidyCanvas(size, size);
            int count = Mathf.Max(2, tiles);
            count += count % 2;
            float grout = Mathf.Clamp(groutWidth, 0f, TileGroutMaximum);
            canvas.Fill((u, v) => TilePixel(u, v, canvas.Width, count, grout, a, b, seed));
            return canvas.ToTexture("FloorTiles");
        }

        private static Color PlankPixel(float u, float v, int size, int planks, Color light, Color dark, int seed)
        {
            float across = u * planks;
            int plank = Mathf.Min(Mathf.FloorToInt(across), planks - 1);
            float localU = across - plank;
            float alongRaw = v * BoardsPerPlank + TidyNoise.Hash01(plank, BoardShiftSalt, seed);
            int board = TidyMath.Wrap(Mathf.FloorToInt(alongRaw), BoardsPerPlank);
            float along = TidyMath.Frac(alongRaw);
            int boardHash = TidyNoise.Hash(plank, board, seed);
            float offset = boardHash % GrainOffsetRange;
            float grain = TidyNoise.Fbm(across * PlankGrainCellsAcross, v * PlankGrainCellsAlong + offset, seed, 4, planks * PlankGrainCellsAcross, PlankGrainCellsAlong);
            float fibre = TidyNoise.Value(across * PlankFibreCellsAcross, v * PlankFibreCellsAlong + offset, seed + 1, planks * PlankFibreCellsAcross, PlankFibreCellsAlong);
            float ringPhase = localU * PlankRingFrequency + grain * PlankRingWarp + TidyNoise.Hash01(board, plank, seed);
            float ring = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(ringPhase * TidyMath.TwoPi), 3f);
            float tone = TidyMath.Saturate(0.25f + 0.5f * grain + 0.25f * fibre - 0.3f * ring);
            float plankPixels = size / (float)planks;
            float boardPixels = size / (float)BoardsPerPlank;
            tone *= KnotShade(localU, along, plankPixels, boardPixels, plank, board, seed);
            float tint = Mathf.Lerp(PlankMinTint, PlankMaxTint, TidyNoise.Hash01(plank, board + BoardTintSalt, seed));
            float hue = (TidyNoise.Hash01(board + BoardTintSalt, plank, seed) - 0.5f) * PlankHueJitter;
            Color wood = TidyColor.Vary(Color.Lerp(dark, light, tone), hue, 1f, tint);
            float edgeDistance = Mathf.Min(Mathf.Min(localU, 1f - localU) * plankPixels, Mathf.Min(along, 1f - along) * boardPixels);
            return BevelledSeam(wood, TidyColor.Shade(dark, PlankGapDarkness), edgeDistance, size);
        }

        private static float KnotShade(float localU, float along, float plankPixels, float boardPixels, int plank, int board, int seed)
        {
            if (TidyNoise.Hash01(plank, board + KnotSalt, seed) > KnotChance)
            {
                return 1f;
            }

            float knotU = Mathf.Lerp(0.3f, 0.7f, TidyNoise.Hash01(board + KnotSalt, plank, seed));
            float knotV = Mathf.Lerp(0.2f, 0.8f, TidyNoise.Hash01(plank + KnotSalt, board, seed));
            float radius = plankPixels * KnotRadiusFraction;
            var offset = new Vector2((localU - knotU) * plankPixels, (along - knotV) * boardPixels * KnotStretch);
            float distance = offset.magnitude;
            float core = TidyMath.SmoothStep(radius * 1.4f, radius * 0.4f, distance);
            float rings = (0.5f + 0.5f * Mathf.Cos(distance / radius * TidyMath.TwoPi)) * TidyMath.SmoothStep(radius * 3f, radius, distance);
            return 1f - core * 0.55f - rings * 0.12f;
        }

        private static Color BevelledSeam(Color surface, Color gap, float edgeDistance, int size)
        {
            float gapHalf = Mathf.Max(0.75f, size * PlankGapFraction);
            float bevel = Mathf.Lerp(PlankBevelDarkening, 1f, TidyMath.SmoothStep(gapHalf, gapHalf + size * PlankBevelFraction, edgeDistance));
            float mask = TidyMath.SmoothStep(gapHalf - 0.5f, gapHalf + 0.75f, edgeDistance);
            return TidyColor.Opaque(Color.Lerp(gap, TidyColor.Shade(surface, bevel), mask));
        }

        private static Color GrainPixel(float u, float v, Color light, Color dark, int seed)
        {
            float warp = TidyNoise.Fbm(u * 4f, v * 2f, seed, 4, 4, 2);
            float phase = u * GrainRingCount + (warp - 0.5f) * GrainRingWarp;
            float ring = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(phase * TidyMath.TwoPi), 4f);
            float fibre = TidyNoise.Value(u * GrainFibreCellsAcross, v * GrainFibreCellsAlong, seed + 3, GrainFibreCellsAcross, GrainFibreCellsAlong);
            float blotch = TidyNoise.Fbm(u * 3f, v * 3f, seed + 5, 3, 3, 3);
            float tone = Mathf.Lerp(0.85f, 0.35f, ring * 0.7f) + (fibre - 0.5f) * 0.25f;
            tone *= 0.85f + 0.3f * blotch;
            return TidyColor.Opaque(Color.Lerp(dark, light, TidyMath.Saturate(tone)));
        }

        private static Color StonePixel(float u, float v, int size, int rows, int blocks, Color mortar, Color stone, int seed)
        {
            float rowCoordinate = v * rows;
            int row = Mathf.Min(Mathf.FloorToInt(rowCoordinate), rows - 1);
            float localV = rowCoordinate - row;
            float shifted = TidyMath.Frac(u + TidyNoise.Hash01(row, 7, seed)) * blocks;
            int cell = Mathf.FloorToInt(shifted);
            if (shifted < StoneBoundary(cell, row, blocks, seed))
            {
                cell--;
            }
            else if (shifted >= StoneBoundary(cell + 1, row, blocks, seed))
            {
                cell++;
            }

            float left = StoneBoundary(cell, row, blocks, seed);
            float right = StoneBoundary(cell + 1, row, blocks, seed);
            float blockPixels = size / (float)blocks;
            float rowPixels = size / (float)rows;
            float mortarHalf = Mathf.Max(1.5f, size * StoneMortarFraction);
            var local = new Vector2((shifted - (left + right) * 0.5f) * blockPixels, (localV - 0.5f) * rowPixels);
            var half = new Vector2((right - left) * 0.5f * blockPixels - mortarHalf, rowPixels * 0.5f - mortarHalf);
            float corner = Mathf.Min(half.x, half.y) * StoneCornerFraction;
            float edgeNoise = TidyNoise.Fbm(u * StoneEdgeNoiseCells, v * StoneEdgeNoiseCells, seed + 11, 3, StoneEdgeNoiseCells, StoneEdgeNoiseCells) - 0.5f;
            float distance = TidySdf.RoundedBox(local, half, corner) + edgeNoise * size * StoneEdgeNoiseFraction * 2f;
            int blockHash = TidyNoise.Hash(TidyMath.Wrap(cell, blocks), row, seed);
            Color surface = StoneSurface(u, v, stone, blockHash, seed);
            float edgeShade = Mathf.Lerp(0.72f, 1f, TidyMath.SmoothStep(0f, size * StoneEdgeShadeFraction, -distance));
            float grit = TidyNoise.Value(u * size * 0.25f, v * size * 0.25f, seed + 13, Mathf.Max(1, size / 4), Mathf.Max(1, size / 4));
            Color joint = TidyColor.Shade(mortar, 0.85f + 0.3f * grit);
            return TidyColor.Opaque(Color.Lerp(joint, TidyColor.Shade(surface, edgeShade), TidyMath.Saturate(0.5f - distance)));
        }

        private static float StoneBoundary(int index, int row, int blocks, int seed)
        {
            int wrapped = TidyMath.Wrap(index, blocks);
            float jitter = (TidyNoise.Hash01(wrapped, row + 101, seed) - 0.5f) * StoneJitter;
            return index + jitter;
        }

        private static Color StoneSurface(float u, float v, Color stone, int blockHash, int seed)
        {
            float tint = 0.85f + 0.27f * ((blockHash & 0xFF) / 255f);
            float hue = (((blockHash >> 8) & 0xFF) / 255f - 0.5f) * 0.03f;
            float surface = TidyNoise.Fbm(u * StoneSurfaceCells, v * StoneSurfaceCells, seed + 2, 5, StoneSurfaceCells, StoneSurfaceCells);
            float crack = TidyMath.SmoothStep(0.9f, 0.97f, TidyNoise.Ridged(u * StoneCrackCells, v * StoneCrackCells, seed + 4, 3, StoneCrackCells, StoneCrackCells));
            float tone = (0.8f + 0.4f * surface) * (1f - crack * 0.3f);
            return TidyColor.Vary(stone, hue, 1f, tint * tone);
        }

        private static Color TilePixel(float u, float v, int size, int count, float grout, Color a, Color b, int seed)
        {
            float tu = u * count;
            float tv = v * count;
            int ix = Mathf.Min(Mathf.FloorToInt(tu), count - 1);
            int iy = Mathf.Min(Mathf.FloorToInt(tv), count - 1);
            float lu = tu - ix;
            float lv = tv - iy;
            Color baseColor = ((ix + iy) & 1) == 0 ? a : b;
            float variation = 0.94f + 0.12f * TidyNoise.Hash01(ix, iy, seed);
            float mottling = TidyNoise.Fbm(u * count * 2f, v * count * 2f, seed + 1, 4, count * 2, count * 2);
            Color tile = TidyColor.Shade(baseColor, variation * (0.92f + 0.16f * mottling));
            float edge = Mathf.Min(Mathf.Min(lu, 1f - lu), Mathf.Min(lv, 1f - lv));
            float groutHalf = grout * 0.5f;
            float pixel = count / (float)size;
            float bevel = Mathf.Lerp(0.85f, 1f, TidyMath.SmoothStep(groutHalf, groutHalf + TileBevelWidth, edge));
            float mask = TidyMath.SmoothStep(groutHalf, groutHalf + pixel * 1.5f, edge);
            float groutNoise = TidyNoise.Value(u * size * 0.25f, v * size * 0.25f, seed + 2, Mathf.Max(1, size / 4), Mathf.Max(1, size / 4));
            Color groutColor = TidyColor.Shade(Color.Lerp(a, b, 0.5f), TileGroutDarkness * (0.9f + 0.2f * groutNoise));
            return TidyColor.Opaque(Color.Lerp(groutColor, TidyColor.Shade(tile, bevel), mask));
        }
    }
}
