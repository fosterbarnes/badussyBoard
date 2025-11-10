using System.Windows.Media;

namespace BadussyBoard
{
    public class AnimateButton
    {
        public static void Press(ScaleTransform transform)
        {
            if (transform != null)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 0.90,
                    Duration = TimeSpan.FromMilliseconds(50),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        public static void Release(ScaleTransform transform)
        {
            if (transform != null)
            {
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }
    }
}
