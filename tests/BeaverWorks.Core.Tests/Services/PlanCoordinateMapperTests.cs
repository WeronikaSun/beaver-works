using BeaverWorks.Core.Services;

namespace BeaverWorks.Core.Tests.Services;

public class PlanCoordinateMapperTests
{
    // Control is wider than the image's aspect ratio -> horizontal letterboxing (margins on left/right).
    private const double WideControlWidth = 400;
    private const double WideControlHeight = 200;
    private const double NarrowImageWidth = 100;
    private const double NarrowImageHeight = 100;

    // Control is taller than the image's aspect ratio -> vertical letterboxing (margins on top/bottom).
    private const double TallControlWidth = 200;
    private const double TallControlHeight = 400;

    [Fact]
    public void TryMapClickToPlan_WideControlNarrowImage_ClickInsideImage_MapsToExpectedNormalizedPoint()
    {
        // scale = min(400/100, 200/100) = 2 -> rendered image is 200x200, centered horizontally: offsetX = 100, offsetY = 0
        var result = PlanCoordinateMapper.TryMapClickToPlan(
            WideControlWidth, WideControlHeight, NarrowImageWidth, NarrowImageHeight,
            clickX: 200, clickY: 100);

        Assert.NotNull(result);
        Assert.Equal(0.5, result!.X, precision: 10);
        Assert.Equal(0.5, result.Y, precision: 10);
    }

    [Fact]
    public void TryMapClickToPlan_WideControlNarrowImage_ClickInLetterboxMargin_ReturnsNull()
    {
        // x=50 is inside the left margin (rendered image starts at x=100)
        var result = PlanCoordinateMapper.TryMapClickToPlan(
            WideControlWidth, WideControlHeight, NarrowImageWidth, NarrowImageHeight,
            clickX: 50, clickY: 100);

        Assert.Null(result);
    }

    [Fact]
    public void TryMapClickToPlan_TallControlNarrowImage_ClickInLetterboxMargin_ReturnsNull()
    {
        // scale = min(200/100, 400/100) = 2 -> rendered image is 200x200, centered vertically: offsetY = 100
        var result = PlanCoordinateMapper.TryMapClickToPlan(
            TallControlWidth, TallControlHeight, NarrowImageWidth, NarrowImageHeight,
            clickX: 100, clickY: 50);

        Assert.Null(result);
    }

    [Fact]
    public void TryMapClickToPlan_ClickExactlyOnImageBoundary_IsTreatedAsInside()
    {
        // Rendered image rect for the wide-control case is x in [100, 300], y in [0, 200].
        var topLeft = PlanCoordinateMapper.TryMapClickToPlan(
            WideControlWidth, WideControlHeight, NarrowImageWidth, NarrowImageHeight,
            clickX: 100, clickY: 0);
        var bottomRight = PlanCoordinateMapper.TryMapClickToPlan(
            WideControlWidth, WideControlHeight, NarrowImageWidth, NarrowImageHeight,
            clickX: 300, clickY: 200);

        Assert.NotNull(topLeft);
        Assert.Equal(0.0, topLeft!.X, precision: 10);
        Assert.Equal(0.0, topLeft.Y, precision: 10);

        Assert.NotNull(bottomRight);
        Assert.Equal(1.0, bottomRight!.X, precision: 10);
        Assert.Equal(1.0, bottomRight.Y, precision: 10);
    }

    [Fact]
    public void MapPlanToControl_RoundTripsTryMapClickToPlanResult()
    {
        var original = PlanCoordinateMapper.TryMapClickToPlan(
            WideControlWidth, WideControlHeight, NarrowImageWidth, NarrowImageHeight,
            clickX: 150, clickY: 75)!;

        var backToControl = PlanCoordinateMapper.MapPlanToControl(
            WideControlWidth, WideControlHeight, NarrowImageWidth, NarrowImageHeight,
            original);

        Assert.NotNull(backToControl);
        Assert.Equal(150, backToControl!.Value.X, precision: 10);
        Assert.Equal(75, backToControl.Value.Y, precision: 10);
    }

    [Fact]
    public void TryMapClickToPlan_ZeroOrNegativeDimensions_ReturnsNullInsteadOfNaN()
    {
        var result = PlanCoordinateMapper.TryMapClickToPlan(
            controlWidth: 0, controlHeight: 200, imagePixelWidth: 100, imagePixelHeight: 100,
            clickX: 10, clickY: 10);

        Assert.Null(result);
    }
}
