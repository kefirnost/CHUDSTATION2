using System;
using System.Numerics;
using Content.Client._CHUD.Graphics.Bloom;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Tests.Client._CHUD.Graphics;

[TestFixture]
[TestOf(typeof(ChudBloomOverlay))]
public sealed class ChudBloomOverlayTest
{
    [TestCase((int) ChudBloomQuality.Low, 8)]
    [TestCase((int) ChudBloomQuality.Medium, 4)]
    [TestCase((int) ChudBloomQuality.High, 2)]
    public void QualityControlsBloomResolution(int quality, int expectedDivisor)
    {
        Assert.That(ChudBloomOverlay.GetResolutionDivisor((ChudBloomQuality) quality), Is.EqualTo(expectedDivisor));
    }

    [Test]
    public void LowUsesQuarterOfMediumBlurPixels()
    {
        var viewport = new Vector2i(1920, 1080);
        var low = viewport / ChudBloomOverlay.GetResolutionDivisor(ChudBloomQuality.Low);
        var medium = viewport / ChudBloomOverlay.GetResolutionDivisor(ChudBloomQuality.Medium);

        Assert.That(low.X * low.Y, Is.EqualTo(medium.X * medium.Y / 4));
    }

    [Test]
    public void AtlasRetainsCapacityWhenSourcesLeaveView()
    {
        var original = ChudBloomOverlay.GetAtlasLayout(5, new Vector2i(64, 64), 4, default);
        var reduced = ChudBloomOverlay.GetAtlasLayout(1, new Vector2i(48, 48), 4, original);

        Assert.That(reduced, Is.EqualTo(original));
    }

    [TestCase(256, 256)]
    [TestCase(256, 64)]
    [TestCase(64, 256)]
    [TestCase(128, 128)]
    public void AtlasRestoresBatchCapacityAfterLargeSourceLeavesView(int width, int height)
    {
        var original = ChudBloomOverlay.GetAtlasLayout(16, new Vector2i(width, height), 4, default);
        var reduced = ChudBloomOverlay.GetAtlasLayout(16, new Vector2i(64, 64), 4, original);

        Assert.That(reduced.CellSize, Is.EqualTo(new Vector2i(64, 64)));
        Assert.That(reduced.Grid, Is.EqualTo(new Vector2i(4, 4)),
            "All sixteen remaining sources must fit in one batch after the large source leaves.");
    }

    [Test]
    public void AtlasShrinksOversizedAxisWhileAnotherAxisGrows()
    {
        var original = ChudBloomOverlay.GetAtlasLayout(16, new Vector2i(256, 64), 4, default);
        var changed = ChudBloomOverlay.GetAtlasLayout(16, new Vector2i(64, 96), 4, original);

        Assert.That(changed.CellSize, Is.EqualTo(new Vector2i(64, 96)));
        Assert.That(changed.Grid, Is.EqualTo(new Vector2i(4, 2)));
    }

    [TestCase(18, 3)]
    [TestCase(3, 18)]
    public void AtlasRebuildsLayoutWhenZoomChangesPadding(int previousPadding, int padding)
    {
        var original = ChudBloomOverlay.GetAtlasLayout(5, new Vector2i(64, 64), 4, default, previousPadding);
        var zoomed = ChudBloomOverlay.GetAtlasLayout(1, new Vector2i(48, 48), 4, original, padding);

        Assert.That(zoomed.CellSize, Is.EqualTo(new Vector2i(48, 48)));
        Assert.That(zoomed.Grid, Is.EqualTo(Vector2i.One));
    }

    [Test]
    public void AtlasGrowsWhenAdditionalSourcesNeedSpace()
    {
        var original = ChudBloomOverlay.GetAtlasLayout(4, new Vector2i(64, 64), 4, default);
        var expanded = ChudBloomOverlay.GetAtlasLayout(5, new Vector2i(64, 64), 4, original);

        Assert.That(expanded.CellSize, Is.EqualTo(original.CellSize));
        Assert.That(expanded.Grid.X * expanded.Grid.Y, Is.GreaterThanOrEqualTo(5));
        Assert.That(ChudBloomOverlay.GetAtlasLayout(4, new Vector2i(64, 64), 4, expanded), Is.EqualTo(expanded));
    }

    [Test]
    public void LargerAtlasCellsStillRespectTextureBudget()
    {
        var original = ChudBloomOverlay.GetAtlasLayout(16, new Vector2i(64, 64), 4, default);
        var expanded = ChudBloomOverlay.GetAtlasLayout(16, new Vector2i(96, 96), 4, original);

        Assert.That(expanded.CellSize, Is.EqualTo(new Vector2i(96, 96)));
        Assert.That(expanded.CellSize.X * expanded.Grid.X * 4, Is.LessThanOrEqualTo(1024));
        Assert.That(expanded.CellSize.Y * expanded.Grid.Y * 4, Is.LessThanOrEqualTo(1024));
    }

    [TestCase(8, 4)]
    [TestCase(4, 2)]
    [TestCase(2, 2)]
    public void DownsamplingCoversEverySourceTexel(int divisor, int expectedAxisSamples)
    {
        var sourceSize = new Vector2(1024, 512);
        var samples = ChudBloomOverlay.GetDownsampleAxisSamples(divisor);
        var step = ChudBloomOverlay.GetDownsampleStep(divisor, sourceSize);
        var covered = new bool[divisor];

        Assert.That(samples, Is.EqualTo(expectedAxisSamples));
        for (var sample = 0; sample < samples; sample++)
        {
            var offset = (sample * 2 - samples + 1) * step.X * sourceSize.X;
            var samplePosition = divisor / 2f + offset - 0.5f;
            covered[(int) MathF.Floor(samplePosition)] = true;
            covered[(int) MathF.Ceiling(samplePosition)] = true;
        }

        Assert.That(covered, Is.All.True);
        Assert.That(step.X * sourceSize.X, Is.EqualTo(step.Y * sourceSize.Y));
    }

    [TestCase(4f, 4f, 3)]
    [TestCase(8f, 16f, 6)]
    [TestCase(1000f, 1000f, 18)]
    public void BlurPaddingContainsKernelAndFilterBorder(float horizontalScale, float verticalScale, int expected)
    {
        var padding = ChudBloomOverlay.GetTargetPadding(new Vector2(horizontalScale, verticalScale));

        Assert.That(padding, Is.EqualTo(expected));
        Assert.That(padding, Is.GreaterThan(ChudBloomOverlay.CalculateBlurRadius(ChudBloomQuality.Medium, horizontalScale)));
        Assert.That(padding, Is.GreaterThan(ChudBloomOverlay.CalculateBlurRadius(ChudBloomQuality.Medium, verticalScale)));
    }

    [Test]
    public void LowPaddingDoesNotIncreaseFullResolutionSpriteCell()
    {
        var lowScale = new Vector2(32f / 8);
        var mediumScale = new Vector2(32f / 4);
        var lowBounds = Box2.FromDimensions(Vector2.Zero, lowScale);
        var mediumBounds = Box2.FromDimensions(Vector2.Zero, mediumScale);
        var low = ChudBloomOverlay.GetBloomTargetSize(lowBounds, ChudBloomOverlay.GetTargetPadding(lowScale)) * 8;
        var medium = ChudBloomOverlay.GetBloomTargetSize(mediumBounds, ChudBloomOverlay.GetTargetPadding(mediumScale)) * 4;

        Assert.That(low.X * low.Y, Is.LessThanOrEqualTo(medium.X * medium.Y));
    }

    [TestCase(1, 64, 64, 4, 1, 1)]
    [TestCase(100, 64, 64, 4, 4, 4)]
    [TestCase(100, 64, 64, 2, 8, 8)]
    [TestCase(100, 512, 128, 4, 1, 2)]
    [TestCase(0, 64, 64, 4, 1, 1)]
    public void BloomAtlasBatchesSourcesWithinTextureBudget(
        int count, int width, int height, int divisor, int columns, int rows)
    {
        var grid = ChudBloomOverlay.GetAtlasGrid(count, new Vector2i(width, height), divisor);
        Assert.That(grid, Is.EqualTo(new Vector2i(columns, rows)));
        Assert.That(grid.X * width * divisor, Is.LessThanOrEqualTo(Math.Max(1024, width * divisor)));
        Assert.That(grid.Y * height * divisor, Is.LessThanOrEqualTo(Math.Max(1024, height * divisor)));
    }

    [TestCase(2, 0u, 0f, 1, 1, 100u, 100f, 2, 1)]
    [TestCase(1, 0u, 100f, 2, 1, 1u, 0f, 1, -1)]
    [TestCase(1, 0u, 20f, 1, 1, 0u, 10f, 2, 1)]
    [TestCase(1, 0u, 10f, 1, 1, 0u, 10f, 2, -1)]
    [TestCase(1, 0u, 10f, 1, 1, 0u, 10f, 1, 0)]
    public void BloomOcclusionFollowsSpriteDrawOrder(
        int sourceDepth, uint sourceOrder, float sourceY, int sourceUid,
        int blockerDepth, uint blockerOrder, float blockerY, int blockerUid, int expected)
    {
        var comparison = ChudBloomOverlay.CompareDrawOrder(
            sourceDepth, sourceOrder, sourceY, new EntityUid(sourceUid),
            blockerDepth, blockerOrder, blockerY, new EntityUid(blockerUid));
        Assert.That(Math.Sign(comparison), Is.EqualTo(expected));
    }

    [Test]
    public void WallFovMaskDrawsAboveWallMountedFixtures()
    {
        Assert.That(
            (int) Content.Shared.DrawDepth.DrawDepth.WallFovOverlay,
            Is.GreaterThan((int) Content.Shared.DrawDepth.DrawDepth.WallMountedItems));
        Assert.That(
            (int) Content.Shared.DrawDepth.DrawDepth.WallFovOverlay,
            Is.LessThan((int) Content.Shared.DrawDepth.DrawDepth.LargeObjects));
    }

    [TestCase(8.2f, 9.9f)]
    [TestCase(63.9f, 32.1f)]
    public void LocalBloomTargetContainsSourceAndBlurPadding(float width, float height)
    {
        var bounds = Box2.FromDimensions(new Vector2(10.9f, 20.1f), new Vector2(width, height));
        var size = ChudBloomOverlay.GetBloomTargetSize(bounds);
        var padding = ChudBloomOverlay.TargetPadding;
        Assert.That(size.X, Is.GreaterThanOrEqualTo(MathF.Ceiling(width) + 2 * padding));
        Assert.That(size.Y, Is.GreaterThanOrEqualTo(MathF.Ceiling(height) + 2 * padding));
        Assert.That(size.X % 16, Is.Zero);
        Assert.That(size.Y % 16, Is.Zero);
    }

    [Test]
    public void SourceReturnsToSameWorldPositionAfterEmissionMaskRoundTrip()
    {
        var sourcePosition = new Vector2(12.5f, -7.25f);
        var layerMatrix = Matrix3x2.CreateTranslation(0.1f, 0.2f);
        var spriteMatrix = Matrix3x2.CreateScale(1.25f);
        var worldMatrix = layerMatrix * spriteMatrix *
                          Matrix3Helpers.CreateTransform(sourcePosition, Angle.FromDegrees(15f));

        var firstWorldToTarget = Matrix3x2.CreateTranslation(-4f, 9f) * Matrix3x2.CreateScale(16f, -16f);
        var secondWorldToTarget = Matrix3x2.CreateTranslation(8f, -3f) * Matrix3x2.CreateScale(24f, -24f);

        Assert.That(Matrix3x2.Invert(firstWorldToTarget, out var firstTargetToWorld), Is.True);
        Assert.That(Matrix3x2.Invert(secondWorldToTarget, out var secondTargetToWorld), Is.True);

        var firstTarget = ChudBloomOverlay.GetSourceToTargetMatrix(
            layerMatrix,
            spriteMatrix,
            sourcePosition,
            Angle.FromDegrees(15f),
            firstWorldToTarget);
        var secondTarget = ChudBloomOverlay.GetSourceToTargetMatrix(
            layerMatrix,
            spriteMatrix,
            sourcePosition,
            Angle.FromDegrees(15f),
            secondWorldToTarget);
        var firstRoundTrip = Vector2.Transform(Vector2.Transform(Vector2.Zero, firstTarget), firstTargetToWorld);
        var secondRoundTrip = Vector2.Transform(Vector2.Transform(Vector2.Zero, secondTarget), secondTargetToWorld);
        var expected = Vector2.Transform(Vector2.Zero, worldMatrix);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstRoundTrip.X, Is.EqualTo(expected.X).Within(0.0001f));
            Assert.That(firstRoundTrip.Y, Is.EqualTo(expected.Y).Within(0.0001f));
            Assert.That(secondRoundTrip.X, Is.EqualTo(expected.X).Within(0.0001f));
            Assert.That(secondRoundTrip.Y, Is.EqualTo(expected.Y).Within(0.0001f));
        }
    }

    [TestCase(0f, 0f)]
    [TestCase(-1f, 0f)]
    [TestCase(float.NaN, 0f)]
    [TestCase(0.4f, 1.4f)]
    [TestCase(1f, 5f)]
    [TestCase(2f, 15f)]
    public void BloomStrengthUsesPerceptualCurve(float configured, float expected)
    {
        Assert.That(ChudBloomOverlay.GetBloomStrength(configured), Is.EqualTo(expected).Within(0.0001f));
    }

    [TestCase((int) ChudBloomQuality.Low, 8f, 1.76f)]
    [TestCase((int) ChudBloomQuality.Medium, 8f, 1.76f)]
    [TestCase((int) ChudBloomQuality.High, 16f, 3.52f)]
    [TestCase((int) ChudBloomQuality.High, 1000f, 220f)]
    [TestCase((int) ChudBloomQuality.Low, 0.5f, 0.11f)]
    public void BlurRadiusFitsPaddedMask(int quality, float pixelsPerMeter, float expected)
    {
        var radius = ChudBloomOverlay.CalculateBlurRadius((ChudBloomQuality) quality, pixelsPerMeter);
        Assert.That(radius, Is.EqualTo(expected).Within(0.0001f));
        var padding = ChudBloomOverlay.GetTargetPadding(new Vector2(pixelsPerMeter));
        Assert.That(radius, Is.LessThan(padding));
    }

    [TestCase(1920, 1080, 4, 0f, 18)]
    [TestCase(3441, 1441, 4, 37f, 18)]
    [TestCase(801, 601, 2, 90f, 18)]
    [TestCase(1921, 1081, 8, 17f, 3)]
    [TestCase(1920, 1080, 4, 37f, 4)]
    [TestCase(801, 601, 2, 90f, 6)]
    public void PaddedMaskCornersCompositeToOriginalViewport(int width, int height, int divisor, float rotation, int paddingSize)
    {
        var viewportSize = new Vector2(width, height);
        var contentSize = new Vector2(width / divisor, height / divisor);
        var camera = Matrix3x2.CreateTranslation(-17.2f, 23.4f) *
                     Matrix3x2.CreateRotation((float) Angle.FromDegrees(rotation)) *
                     Matrix3x2.CreateScale(48f, -48f) * Matrix3x2.CreateTranslation(viewportSize / 2f);
        var mask = ChudBloomOverlay.GetWorldToTargetMatrix(camera, viewportSize, contentSize, paddingSize);
        Assert.That(Matrix3x2.Invert(mask, out var composite), Is.True);
        var padding = new Vector2(paddingSize);
        var topLeft = Vector2.Transform(Vector2.Transform(padding, composite), camera);
        var bottomRight = Vector2.Transform(Vector2.Transform(contentSize + padding, composite), camera);
        Assert.That(Vector2.Distance(topLeft, Vector2.Zero), Is.LessThan(0.001f));
        Assert.That(Vector2.Distance(bottomRight, viewportSize), Is.LessThan(0.001f));
    }

}
