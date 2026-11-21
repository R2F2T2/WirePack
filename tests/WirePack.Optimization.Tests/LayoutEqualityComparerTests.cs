using WirePack.Core.Geometry;

namespace WirePack.Optimization.Tests;

public class LayoutEqualityComparerTests
{
    private static readonly LayoutEqualityComparer Comparer = new();

    private static Layout MakeLayout(params (double X, double Y, double D)[] wires)
    {
        return Layout.FromWires(
            wires.Select(w => new Wire(new Point(w.X, w.Y), w.D)).ToArray());
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        var layout = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));

        Assert.True(Comparer.Equals(layout, layout));
    }

    [Fact]
    public void Equals_NullInputs_ReturnsFalse()
    {
        var layout = MakeLayout((0d, 0d, 2d));

        Assert.False(Comparer.Equals(null, layout));
        Assert.False(Comparer.Equals(layout, null));
        Assert.True(Comparer.Equals(null, null));
    }

    [Fact]
    public void Equals_IdenticalLayouts_ReturnsTrue()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 3d));
        var b = MakeLayout((0d, 0d, 2d), (3d, 0d, 3d));

        Assert.True(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_DifferentWireCounts_ReturnsFalse()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));
        var b = MakeLayout((0d, 0d, 2d));

        Assert.False(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_DifferentDiameters_ReturnsFalse()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));
        var b = MakeLayout((0d, 0d, 3d), (3d, 0d, 2d));

        Assert.False(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_TranslationInvariant_ReturnsTrue()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));
        var b = MakeLayout((2d, 2d, 2d), (5d, 2d, 2d));

        Assert.True(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_RotationInvariant_ReturnsTrue()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));
        // Поворот на 90 градусов вокруг начала координат
        var b = MakeLayout((0d, 0d, 2d), (0d, 3d, 2d));

        Assert.True(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_ReflectionInvariant_ReturnsTrue()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));
        // Отражение по Y
        var b = MakeLayout((0d, 0d, 2d), (-3d, 0d, 2d));

        Assert.True(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_WirePermutation_ReturnsTrue()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));
        var b = MakeLayout((3d, 0d, 2d), (0d, 0d, 2d));

        Assert.True(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_TrulyDifferentLayouts_ReturnsFalse()
    {
        // Два провода одинакового диаметра, но с разной геометрией
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));
        var b = MakeLayout((0d, 0d, 2d), (5d, 0d, 2d));

        Assert.False(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_RotationWithReflectionProducesSame_ReturnsTrue()
    {
        // Асимметричный набор из трёх проводов — проверка комбинированных преобразований
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d), (1d, 2d, 3d));
        // Отражение по X и поворот на 180°
        var b = MakeLayout((0d, 0d, 2d), (-1d, -2d, 3d), (-3d, 0d, 2d));

        Assert.True(Comparer.Equals(a, b));
    }

    [Fact]
    public void GetHashCode_EqualLayouts_HaveEqualHashCodes()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 3d));
        var b = MakeLayout((2d, 2d, 2d), (5d, 2d, 3d));

        Assert.Equal(Comparer.GetHashCode(a), Comparer.GetHashCode(b));
    }

    [Fact]
    public void GetHashCode_DifferentLayouts_AreConsistentWithEquals()
    {
        var a = MakeLayout((0d, 0d, 2d), (3d, 0d, 2d));
        var b = MakeLayout((0d, 0d, 2d), (5d, 0d, 2d));

        // Equals возвращает false для разных компоновок
        Assert.False(Comparer.Equals(a, b));
        // Хеш-код корректен (не нулевой)
        Assert.NotEqual(0, Comparer.GetHashCode(a));
        Assert.NotEqual(0, Comparer.GetHashCode(b));
        // Контракт: если Equals true, то хеши равны; обратное не обязательно
    }

    [Fact]
    public void Equals_SingleWire_TranslationInvariant()
    {
        var a = MakeLayout((1d, 2d, 3d));
        var b = MakeLayout((4d, 5d, 3d));

        Assert.True(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_SingleWire_DifferentDiameter_ReturnsFalse()
    {
        var a = MakeLayout((1d, 2d, 3d));
        var b = MakeLayout((1d, 2d, 4d));

        Assert.False(Comparer.Equals(a, b));
    }

    [Fact]
    public void Equals_AllWiresAtOrigin_ReturnsTrue()
    {
        // Все провода в начале координат разных диаметров
        var a = MakeLayout((0d, 0d, 2d), (0d, 0d, 3d));
        var b = MakeLayout((0d, 0d, 3d), (0d, 0d, 2d));

        Assert.True(Comparer.Equals(a, b));
    }
}