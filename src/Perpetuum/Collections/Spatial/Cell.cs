namespace Perpetuum.Collections.Spatial
{
    public abstract class Cell
    {
        private readonly Area _boundingBox;

        protected Cell(Area boundingBox)
        {
            _boundingBox = boundingBox;
        }

        public Area BoundingBox
        {
            get { return _boundingBox; }
        }

        public override string ToString()
        {
            return string.Format((string) "BoundingBox: {0}", (object) BoundingBox);
        }
    }
}