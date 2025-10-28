namespace Orderlyze.Foundation.Interfaces
{
    public interface IPositionBase
    {
        decimal Xposition { get; set; }
        decimal Yposition { get; set; }
        decimal Width { get; set; }
        decimal Height { get; set; }
        decimal Rotation { get; set; }
        string Color { get; set; }
    }
}
