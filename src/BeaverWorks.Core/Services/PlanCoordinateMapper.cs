using BeaverWorks.Core.Models;

namespace BeaverWorks.Core.Services;

/// <summary>
/// Pure, rendering-framework-free math for the letterboxed image
/// coordinate transform: converts between a control-relative click
/// position and a <see cref="PlanPoint"/> normalized to the plan image's
/// own content area, accounting for the empty margins a
/// <c>Stretch="Uniform"</c>-style image control leaves on one axis when the
/// control's aspect ratio doesn't match the image's.
/// </summary>
public static class PlanCoordinateMapper
{
    /// <summary>
    /// Maps a click at (<paramref name="clickX"/>, <paramref name="clickY"/>),
    /// expressed in control-relative pixels, to a normalized
    /// <see cref="PlanPoint"/>. Returns <see langword="null"/> when the
    /// click falls in the letterbox margin (outside the actual rendered
    /// image rectangle) rather than clamping it onto the image edge.
    /// Coordinates exactly on the image boundary are treated as inside.
    /// </summary>
    public static PlanPoint? TryMapClickToPlan(
        double controlWidth,
        double controlHeight,
        double imagePixelWidth,
        double imagePixelHeight,
        double clickX,
        double clickY)
    {
        var rect = GetRenderedImageRect(controlWidth, controlHeight, imagePixelWidth, imagePixelHeight);
        if (rect is null)
        {
            return null;
        }

        var (offsetX, offsetY, renderedWidth, renderedHeight) = rect.Value;

        var localX = clickX - offsetX;
        var localY = clickY - offsetY;

        if (localX < 0 || localX > renderedWidth || localY < 0 || localY > renderedHeight)
        {
            return null;
        }

        return new PlanPoint
        {
            X = localX / renderedWidth,
            Y = localY / renderedHeight,
        };
    }

    /// <summary>
    /// Reverse of <see cref="TryMapClickToPlan"/>: maps a normalized
    /// <see cref="PlanPoint"/> back to a control-relative pixel position,
    /// for placing a marker over the rendered image.
    /// </summary>
    public static (double X, double Y)? MapPlanToControl(
        double controlWidth,
        double controlHeight,
        double imagePixelWidth,
        double imagePixelHeight,
        PlanPoint point)
    {
        var rect = GetRenderedImageRect(controlWidth, controlHeight, imagePixelWidth, imagePixelHeight);
        if (rect is null)
        {
            return null;
        }

        var (offsetX, offsetY, renderedWidth, renderedHeight) = rect.Value;

        return (offsetX + (point.X * renderedWidth), offsetY + (point.Y * renderedHeight));
    }

    private static (double OffsetX, double OffsetY, double RenderedWidth, double RenderedHeight)? GetRenderedImageRect(
        double controlWidth,
        double controlHeight,
        double imagePixelWidth,
        double imagePixelHeight)
    {
        if (controlWidth <= 0 || controlHeight <= 0 || imagePixelWidth <= 0 || imagePixelHeight <= 0)
        {
            return null;
        }

        var scale = Math.Min(controlWidth / imagePixelWidth, controlHeight / imagePixelHeight);
        var renderedWidth = imagePixelWidth * scale;
        var renderedHeight = imagePixelHeight * scale;
        var offsetX = (controlWidth - renderedWidth) / 2;
        var offsetY = (controlHeight - renderedHeight) / 2;

        return (offsetX, offsetY, renderedWidth, renderedHeight);
    }
}
