namespace WirePack.Core.Tests;

public class LayoutValidatorTests
{
    [Fact]
    public void Validate_ValidLayout_ReturnsSuccess()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(3d, 0d, 2d),
            TestHelpers.Wire(0d, 3d, 2d));
        var tube = TestHelpers.Tube(0d, 0d, 20d);

        Assert.True(LayoutValidator.Validate(layout, tube).IsValid);
    }

    [Fact]
    public void Validate_IntersectingWires_ReturnsIntersectionIssue()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(1d, 0d, 2d));
        var tube = TestHelpers.Tube(0d, 0d, 20d);

        LayoutValidationResult result = LayoutValidator.Validate(layout, tube);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Kind == ValidationIssueKind.WireIntersection);
    }

    [Fact]
    public void Validate_TouchingWires_ReturnsSuccess()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(2d, 0d, 2d));
        var tube = TestHelpers.Tube(0d, 0d, 20d);

        Assert.True(LayoutValidator.Validate(layout, tube).IsValid);
    }

    [Fact]
    public void Validate_WireOutsideTube_ReturnsOutsideIssue()
    {
        var layout = Layout.FromWires(TestHelpers.Wire(9.5d, 0d, 2d));
        var tube = TestHelpers.Tube(0d, 0d, 20d);

        LayoutValidationResult result = LayoutValidator.Validate(layout, tube);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Kind == ValidationIssueKind.WireOutsideTube);
    }

    [Fact]
    public void Validate_WireTouchingTube_ReturnsSuccess()
    {
        var layout = Layout.FromWires(TestHelpers.Wire(9d, 0d, 2d));
        var tube = TestHelpers.Tube(0d, 0d, 20d);

        Assert.True(LayoutValidator.Validate(layout, tube).IsValid);
    }

    [Fact]
    public void ValidateMec_MatchingSavedTube_ReturnsSuccess()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(3d, 0d, 2d),
            TestHelpers.Wire(0d, 3d, 2d));
        MecResult mec = NumericalWeightedMecSolver.Solve(layout);
        var tube = new Tube(mec.Center, mec.Radius);

        Assert.True(LayoutValidator.ValidateMec(layout, tube, mec).IsValid);
    }

    [Fact]
    public void ValidateMec_ShiftedCenter_ReturnsCenterIssue()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(3d, 0d, 2d),
            TestHelpers.Wire(0d, 3d, 2d));
        MecResult mec = NumericalWeightedMecSolver.Solve(layout);
        var tube = new Tube(mec.Center.Translate(1e-6d, 0d), mec.Radius);

        LayoutValidationResult result = LayoutValidator.ValidateMec(layout, tube, mec);

        Assert.Contains(result.Issues, issue => issue.Kind == ValidationIssueKind.MecCenterMismatch);
    }

    [Fact]
    public void ValidateMec_WrongDiameter_ReturnsDiameterIssue()
    {
        var layout = Layout.FromWires(
            TestHelpers.Wire(0d, 0d, 2d),
            TestHelpers.Wire(3d, 0d, 2d),
            TestHelpers.Wire(0d, 3d, 2d));
        MecResult mec = NumericalWeightedMecSolver.Solve(layout);
        var tube = new Tube(mec.Center, mec.Radius + 1e-6d);

        LayoutValidationResult result = LayoutValidator.ValidateMec(layout, tube, mec);

        Assert.Contains(result.Issues, issue => issue.Kind == ValidationIssueKind.MecDiameterMismatch);
    }
}
