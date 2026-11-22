using WirePack.Core.Geometry;

namespace WirePack.Optimization.Tests;

public class InitialLayoutGeneratorTests
{
    private static readonly Wire[] SampleWires =
    {
        new(Point.Origin, 1d),
        new(Point.Origin, 1.5d),
        new(Point.Origin, 2d),
        new(Point.Origin, 2.5d),
        new(Point.Origin, 3d),
        new(Point.Origin, 4d),
        new(Point.Origin, 5d),
        new(Point.Origin, 1d)
    };

    [Fact]
    public void Generate_RejectsEmptyInput()
    {
        var generator = new InitialLayoutGenerator(0);

        Assert.Throws<ArgumentException>(() => generator.Generate(Array.Empty<Wire>()));
    }

    [Fact]
    public void Generate_RejectsNullInput()
    {
        var generator = new InitialLayoutGenerator(0);

        Assert.Throws<ArgumentNullException>(() => generator.Generate(null!));
    }

    [Fact]
    public void Generate_PreservesWireCountAndDiameters()
    {
        var generator = new InitialLayoutGenerator(123);

        Layout layout = generator.Generate(SampleWires);

        Assert.Equal(SampleWires.Length, layout.Count);
        for (int i = 0; i < SampleWires.Length; i++)
            Assert.Equal(SampleWires[i].Diameter, layout[i].Diameter);
    }

    [Fact]
    public void Generate_ProducesNonOverlappingWiresInsideMec()
    {
        var generator = new InitialLayoutGenerator(7);

        Layout layout = generator.Generate(SampleWires);
        MecResult mec = NumericalWeightedMecSolver.Solve(layout);
        var tube = new Tube(mec.Center, mec.Radius);

        LayoutValidationResult validation = LayoutValidator.Validate(layout, tube);

        Assert.True(validation.IsValid, string.Join("; ", validation.Issues.Select(issue => issue.Message)));
    }

    [Fact]
    public void Generate_IsDeterministicForSameSeed()
    {
        var first = new InitialLayoutGenerator(2024);
        var second = new InitialLayoutGenerator(2024);

        for (int i = 0; i < 3; i++)
        {
            Layout firstLayout = first.Generate(SampleWires);
            Layout secondLayout = second.Generate(SampleWires);

            Assert.Equal(Flatten(firstLayout), Flatten(secondLayout));
        }
    }

    [Fact]
    public void Generate_ProducesDifferentSequentialLayouts()
    {
        var generator = new InitialLayoutGenerator(42);

        Layout firstLayout = generator.Generate(SampleWires);
        Layout secondLayout = generator.Generate(SampleWires);

        Assert.False(Flatten(firstLayout).SequenceEqual(Flatten(secondLayout)));
    }

    [Fact]
    public void Generate_ProducesDifferentLayoutsForDifferentSeeds()
    {
        Layout firstLayout = new InitialLayoutGenerator(1).Generate(SampleWires);
        Layout secondLayout = new InitialLayoutGenerator(2).Generate(SampleWires);

        Assert.False(Flatten(firstLayout).SequenceEqual(Flatten(secondLayout)));
    }

    [Fact]
    public void Generate_ProducesDenseInitialLayout()
    {
        Wire[] wires = Enumerable.Range(0, 50)
            .Select(_ => new Wire(Point.Origin, 2d))
            .ToArray();

        Layout layout = new InitialLayoutGenerator(0).Generate(wires);
        var variant = new LayoutVariant(layout);

        Assert.True(variant.FillFactor > 0.4d,
            $"Начальное заполнение слишком низкое: {variant.FillFactor:P1}.");
    }

    [Fact]
    public void Generate_HandlesSingleWire()
    {
        var generator = new InitialLayoutGenerator(5);

        Layout layout = generator.Generate(new[] { new Wire(Point.Origin, 3d) });

        Assert.Equal(1, layout.Count);
        Assert.Equal(3d, layout[0].Diameter);
    }

    [Fact]
    public void Generate_HandlesHundredWires()
    {
        Wire[] wires = Enumerable.Range(0, 100)
            .Select(index => new Wire(Point.Origin, 1d + (index % 5) * 0.5d))
            .ToArray();

        var generator = new InitialLayoutGenerator(11);

        Layout layout = generator.Generate(wires);
        MecResult mec = NumericalWeightedMecSolver.Solve(layout);
        var tube = new Tube(mec.Center, mec.Radius);
        LayoutValidationResult validation = LayoutValidator.Validate(layout, tube);

        Assert.Equal(100, layout.Count);
        Assert.True(validation.IsValid, string.Join("; ", validation.Issues.Select(issue => issue.Message)));
    }

    private static double[] Flatten(Layout layout)
    {
        var result = new double[layout.Count * 2];
        for (int i = 0; i < layout.Count; i++)
        {
            result[i * 2] = layout[i].Center.X;
            result[i * 2 + 1] = layout[i].Center.Y;
        }

        return result;
    }
}
