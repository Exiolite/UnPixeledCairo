
#nullable disable
namespace Cairo
{
    public static class ContextUtils
    {
        /// <summary>
        /// Draw 0,0,0,0 color onto the entire surface
        /// </summary>
        /// <param name="ctx"></param>
        public static void Clear(this Context ctx)
        {
            var prevop = ctx.Operator;
            ctx.SetSourceRGBA(0, 0, 0, 0);
            ctx.Operator = Operator.Source;
            ctx.Paint();
            ctx.Operator = prevop;
        }
    }
}
