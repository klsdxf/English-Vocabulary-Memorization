using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace CET6Word {
 public partial class DesktopUI {
  static readonly Duration Motion=new Duration(TimeSpan.FromMilliseconds(220));
  readonly Dictionary<FrameworkElement,int> animationVersion=new Dictionary<FrameworkElement,int>();
  Image removedImage;Point removedPoint;
  static DoubleAnimation Tween(double from,double to){return new DoubleAnimation(from,to,Motion){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};}
  static void Fade(UIElement item){item.BeginAnimation(UIElement.OpacityProperty,Tween(.55,1));}
  static void Slide(FrameworkElement item,double offset){var transform=new TranslateTransform();item.RenderTransform=transform;transform.BeginAnimation(TranslateTransform.XProperty,Tween(offset,0));Fade(item);}
  static void SlideOut(FrameworkElement item,Action done){var transform=new TranslateTransform();item.RenderTransform=transform;var motion=Tween(0,40);motion.Completed+=(s,e)=>done();transform.BeginAnimation(TranslateTransform.XProperty,motion);item.BeginAnimation(UIElement.OpacityProperty,Tween(1,0));}
  static void Pulse(Button item){item.RenderTransformOrigin=new Point(.5,.5);var transform=new ScaleTransform();item.RenderTransform=transform;var pulse=Tween(1,1.18);pulse.AutoReverse=true;pulse.Duration=new Duration(TimeSpan.FromMilliseconds(110));transform.BeginAnimation(ScaleTransform.ScaleXProperty,pulse);transform.BeginAnimation(ScaleTransform.ScaleYProperty,pulse);var brush=new SolidColorBrush(Color.FromRgb(41,90,182));item.Foreground=brush;brush.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(Colors.SlateGray,Color.FromRgb(41,90,182),Motion));}
  static void Feedback(TextBlock item,bool correct){var brush=new SolidColorBrush(correct?Color.FromRgb(35,125,89):Color.FromRgb(181,71,71));item.Foreground=brush;brush.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(Colors.SlateGray,brush.Color,Motion));Slide(item,8);}
  static IEnumerable<DependencyObject> Tree(DependencyObject root){if(root==null)yield break;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var d in Tree(c))yield return d;}}
  void AnimateRow(WordView row,Action change){var panels=Tree(Window).OfType<FrameworkElement>().Where(x=>x.DataContext==row&&(x.Name=="MeaningPanel"||x.Name=="ExamplePanel"||x.Name=="ExtraPanel")).ToList();var heights=panels.Select(x=>x.Visibility==Visibility.Visible?x.ActualHeight:0).ToList();change();
   for(int i=0;i<panels.Count;i++){var panel=panels[i];bool shown=panel.Visibility==Visibility.Visible;double start=heights[i];if(!shown&&start==0)continue;panel.BeginAnimation(FrameworkElement.HeightProperty,null);panel.Height=Double.NaN;panel.Visibility=Visibility.Visible;panel.Measure(new Size(Math.Max(100,panel.ActualWidth>0?panel.ActualWidth:320),Double.PositiveInfinity));double end=shown?panel.DesiredSize.Height:0;int version;animationVersion.TryGetValue(panel,out version);version++;animationVersion[panel]=version;int captured=version;var height=Tween(start,end);height.Completed+=(s,e)=>{int latest;if(!animationVersion.TryGetValue(panel,out latest)||latest!=captured)return;panel.BeginAnimation(FrameworkElement.HeightProperty,null);panel.Height=Double.NaN;panel.Visibility=shown?Visibility.Visible:Visibility.Collapsed;animationVersion.Remove(panel);};panel.BeginAnimation(FrameworkElement.HeightProperty,height);panel.BeginAnimation(UIElement.OpacityProperty,Tween(start==0?0:1,shown?1:0));}
  }
  void CaptureRemoval(WordView row){removedImage=null;if(studyList==null)return;var container=studyList.ItemContainerGenerator.ContainerFromItem(row) as FrameworkElement;if(container==null||container.ActualWidth<1||container.ActualHeight<1)return;var bitmap=new RenderTargetBitmap((int)Math.Ceiling(container.ActualWidth),(int)Math.Ceiling(container.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(container);removedImage=new Image{Source=bitmap,Width=container.ActualWidth,Height=container.ActualHeight,IsHitTestVisible=false};removedPoint=container.TranslatePoint(new Point(0,0),studyColumns);}
  void FadeRemoved(WordView row){if(removedImage==null)return;var ghost=removedImage;removedImage=null;var overlay=new Canvas{IsHitTestVisible=false,ClipToBounds=true};Grid.SetRow(overlay,1);Panel.SetZIndex(overlay,10);Canvas.SetLeft(ghost,removedPoint.X);Canvas.SetTop(ghost,removedPoint.Y);overlay.Children.Add(ghost);var owner=studyGrid;owner.Children.Add(overlay);var fade=Tween(1,0);fade.Completed+=(s,e)=>owner.Children.Remove(overlay);ghost.BeginAnimation(UIElement.OpacityProperty,fade);}
 }
}
