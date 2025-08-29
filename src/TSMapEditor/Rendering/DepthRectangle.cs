namespace TSMapEditor.Rendering
{
    public struct DepthRectangle
    {
        public float TopLeft;
        public float TopRight;
        public float BottomLeft;
        public float BottomRight;

        public DepthRectangle() { }

        public DepthRectangle(float topLeft, float topRight, float bottomLeft, float bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public DepthRectangle(float top, float bottom) : this(top, top, bottom, bottom) { }

        public static DepthRectangle operator+(DepthRectangle left, float right)
            => new DepthRectangle(left.TopLeft + right, left.TopRight + right, left.BottomLeft + right, left.BottomRight + right);

        public override string ToString()
        {
            return "TL: " + TopLeft + ", TR: " + TopRight + ", BL: " + BottomLeft + ", BR: " + BottomRight;
        }
    }
}
