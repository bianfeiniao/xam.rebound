using Android.Views;
using Android.Widget;
using Android.Content.Res;
using Android.Util;

namespace xam.rebound.android.ui
{
    /**
  * Utilities for generating view hierarchies without using resources.
  */
    public abstract class Util
    {
        public static int dpToPx(float dp, Resources res)
        {
            return (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, res.DisplayMetrics);
        }

        public static FrameLayout.LayoutParams createLayoutParams(int width, int height)
        {
            return new FrameLayout.LayoutParams(width, height);
        }

        public static FrameLayout.LayoutParams createMatchParams()
        {
            return createLayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent);
        }

        public static FrameLayout.LayoutParams createWrapParams()
        {
            return createLayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent);
        }

        public static FrameLayout.LayoutParams createWrapMatchParams()
        {
            return createLayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.MatchParent);
        }

        public static FrameLayout.LayoutParams createMatchWrapParams()
        {
            return createLayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);
        }

    }

}