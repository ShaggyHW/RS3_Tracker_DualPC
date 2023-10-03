using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static DualPC_AbilityTracker.Classes.DisplayClasses;

namespace DualPC_AbilityTracker {
    /// <summary>
    /// Interaction logic for Display.xaml
    /// </summary>
    public partial class Display : Window {


        HttpListener listener = new HttpListener();
        public class requestSource {
            public string Key { get; set; }
            public string Modifier { get; set; }
        }
        private void StartListener() {
            listener.Prefixes.Add("http://*:8086/");

            listener.Start();

            while (true) {
                HttpListenerContext ctx = listener.GetContext();
                using (HttpListenerResponse resp = ctx.Response) {
                    string endpoint = ctx.Request.RawUrl.Replace("/?", "");
                    switch (endpoint) {
                      
                        case "update":
                            requestSource req = new requestSource();
                          
                            string text;
                            using (var reader = new StreamReader(ctx.Request.InputStream,
                                                                 ctx.Request.ContentEncoding)) {
                                text = reader.ReadToEnd();
                            }
                            req = JsonConvert.DeserializeObject<requestSource>(text);
                            HookKeyDown(req);
                            Thread.Sleep(100);
                            break;
                    }
                }
            }
        }


        //Hook KeyboardHook = new Hook("Globalaction Link");
        List<KeybindClass> keybindClasses = new List<KeybindClass>();
        List<BarKeybindClass> keybindBarClasses = new List<BarKeybindClass>();
        int imgCounter = 0;
        public string style = "";
        public List<Keypressed> ListKeypressed = new List<Keypressed>();
        public List<Keypressed> ListPreviousKeypressed = new List<Keypressed>();
        public Stopwatch stopwatch = new Stopwatch();
        public bool control = false;
        private Keypressed previousKey = new Keypressed();
        private List<Keypressed> ListPreviousKeys = new List<Keypressed>();
        private bool trackCD;
        private bool pause = false;


        bool resize = false;

        public Display(string _style, bool trackCD, bool onTop, bool resize) {
            this.resize = resize;
            //this.Left = LeftPos;
            //this.Top = TopPos;
            InitializeComponent();

            AllowsTransparency = false;
            ResizeON();
            //if (resize) {
            //    AllowsTransparency = false;
            //    ResizeON();
            //} else {
            //    AllowsTransparency = true;
            //    ResizeOFF();
            //}
            //this.LeftPos = LeftPos;
            //this.TopPos = TopPos;
            //this.height = height;
            //this.width = width;
            Loaded += Display_Loaded;
            var windowHwnd = new WindowInteropHelper(this).Handle;

            //KeyboardHook.KeyDownEvent += HookKeyDown;
            style = _style;
            TESTLABEL.Content = style;
            keybindClasses = JsonConvert.DeserializeObject<List<KeybindClass>>(File.ReadAllText(".\\keybinds.json"));
            keybindBarClasses = JsonConvert.DeserializeObject<List<BarKeybindClass>>(File.ReadAllText(".\\barkeybinds.json"));
            changeStyle();
            stopwatch.Start();
            previousKey.ability = new Ability();
            Thread timerThread = new Thread(new ThreadStart(StartListener));
            timerThread.Start();
            this.trackCD = trackCD;
            this.Topmost = onTop;
        }


        private void Display_Loaded(object sender, RoutedEventArgs e) {
            if (resize) {

                ResizeON();
            } else {

                ResizeOFF();
            }
            //this.Height = height;
            //this.Width = width;
        }

        //        private void 

        public void ResizeON() {
            this.ResizeMode = ResizeMode.CanResize;

        }

        public void ResizeOFF() {

            this.ResizeMode = ResizeMode.NoResize;
        }

        public void changeStyle() {
            keybindClasses = JsonConvert.DeserializeObject<List<KeybindClass>>(File.ReadAllText(".\\keybinds.json"));
            keybindClasses = keybindClasses.Where(p => p.bar.name.ToLower() == style.ToLower() || p.bar.name.ToLower() == "all").Select(p => p).ToList();
        }

        #region imageProcessing
        private Bitmap Tint(Bitmap bmpSource, System.Drawing.Color clrScaleColor, float sngScaleDepth) {

            Bitmap bmpTemp = new Bitmap(bmpSource.Width, bmpSource.Height); //Create Temporary Bitmap To Work With

            ImageAttributes iaImageProps = new ImageAttributes(); //Contains information about how bitmap and metafile colors are manipulated during rendering. 

            ColorMatrix cmNewColors = default(ColorMatrix); //Defines a 5 x 5 matrix that contains the coordinates for the RGBAW space
            cmNewColors = new ColorMatrix(new float[][] {
                new float[] {
                    1,
                    0,
                    0,
                    0,
                    0
                },
                new float[] {
                    0,
                    1,
                    0,
                    0,
                    0
                },
                new float[] {
                    0,
                    0,
                    1,
                    0,
                    0
                },
                new float[] {
                    0,
                    0,
                    0,
                    1,
                    0
                },
                new float[] {
                    clrScaleColor.R / 255 * sngScaleDepth,
                    clrScaleColor.G / 255 * sngScaleDepth,
                    clrScaleColor.B / 255 * sngScaleDepth,
                    0,
                    1
                }
            });

            iaImageProps.SetColorMatrix(cmNewColors); //Apply Matrix
            Graphics grpGraphics = Graphics.FromImage(bmpTemp); //Create Graphics Object and Draw Bitmap Onto Graphics Object
            grpGraphics.DrawImage(bmpSource, new System.Drawing.Rectangle(0, 0, bmpSource.Width, bmpSource.Height), 0, 0, bmpSource.Width, bmpSource.Height, GraphicsUnit.Pixel, iaImageProps);
            return bmpTemp;
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp) {
            var handle = bmp.GetHbitmap();
            try {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            } finally { DeleteObject(handle); }
        }
        #endregion

        private void HookKeyDown(requestSource key) {
            #region display
            if (!control) {
                control = true;
                Keypressed keypressed = new Keypressed();
                keypressed.ability = new Ability();
                string modifier = "";
                if (key.Key.ToString().ToLower().Equals("none")) {
                    control = false;
                    return;
                }
                modifier = key.Modifier;
                //if (key.isAltPressed)
                //    modifier = "ALT";
                //else if (key.isCtrlPressed)
                //    modifier = "CTRL";
                //else if (key.isShiftPressed)
                //    modifier = "SHIFT";
                //else if (key.isWinPressed)
                //    modifier = "WIN";

                List<Ability> abilityList = (from r in keybindClasses
                                             where r.key.ToLower() == key.Key.ToString().ToLower()
                                             where r.modifier.ToString().ToLower() == modifier.ToLower()
                                             select r.ability).ToList();

                if (abilityList.Count == 0) {
                    if (keybindBarClasses != null) {
                        var listBarChange2 = keybindBarClasses.Where(p => p.key.ToLower().Equals(key.Key.ToString().ToLower()) && p.modifier.ToLower().Equals(modifier.ToLower())).Select(p => p).FirstOrDefault();
                        if (listBarChange2 != null) {
                            if (listBarChange2.name.ToLower().Equals("clear")) {
                                displayImg1.Source = null;
                                displayImg2.Source = null;
                                displayImg3.Source = null;
                                displayImg4.Source = null;
                                displayImg5.Source = null;
                                displayImg6.Source = null;
                                displayImg7.Source = null;
                                displayImg8.Source = null;
                                displayImg9.Source = null;
                                displayImg10.Source = null;
                                control = false;
                                return;
                            } else if (listBarChange2.name.ToLower().Equals("pause")) {
                                pause = !pause;
                            }
                        }
                    }
                }

                if (pause) {
                    control = false;
                    return;
                }

                foreach (var ability in abilityList) {

                    if (ability == null)
                        continue;

                    keypressed.modifier = modifier;
                    keypressed.key = key.Key.ToString();
                    keypressed.ability.name = ability.name;
                    keypressed.ability.img = ability.img;
                    keypressed.ability.cooldown = ability.cooldown;
                    keypressed.timepressed = stopwatch.Elapsed.TotalMilliseconds;

                    for (int i = 0; i < ListPreviousKeypressed.Count; i++) {
                        var prevabil = ListPreviousKeypressed[i];
                        if ((keypressed.timepressed - prevabil.timepressed) > 1200) {
                            ListPreviousKeypressed.RemoveAt(i);
                            i--;
                        }
                    }

                    previousKey = ListPreviousKeypressed.Where(a => a.ability.img.Equals(keypressed.ability.img)).Select(a => a).FirstOrDefault();
                    if (previousKey != null) {
                        control = false;
                        return;
                    }
                    ListKeypressed.Add(keypressed);
                    previousKey = new Keypressed() {
                        timepressed = keypressed.timepressed,
                        ability = new Ability {
                            img = keypressed.ability.img,
                            name = keypressed.ability.name
                        }
                    };
                    ListPreviousKeypressed.Add(previousKey);
                    if (true) {

                    }

                    Bitmap bitmap = new Bitmap(ability.img);
                    Bitmap Image;
                    ImageSource imageSource;
                    if (trackCD) {
                        bool onCD = abilCoolDown(ListPreviousKeys, keypressed);
                        if (onCD) {
                            Image = Tint(bitmap, System.Drawing.Color.Red, 0.5f);
                            imageSource = ImageSourceFromBitmap(Image);
                        } else {
                            imageSource = ImageSourceFromBitmap(bitmap);
                            ListPreviousKeys.Add(previousKey);
                        }
                    } else {
                        imageSource = ImageSourceFromBitmap(bitmap);
                        ListPreviousKeys.Add(previousKey);
                    }

                    //Display
                    imageSource.Freeze();
                    Action updateImage;
                    switch (imgCounter) {
                        case 0:
                             updateImage = () => {
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);

                            break;
                        case 1:
                             updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        case 2:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        case 3:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        case 4:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        case 5:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        case 6:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        case 7:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        case 8:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        case 9:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                        default:
                            updateImage = () => {
                                moveImgs(imgCounter);
                                displayImg10.Source = imageSource;
                            };
                            Dispatcher.BeginInvoke(updateImage);
                            break;
                    }
                    if (imgCounter < 9)
                        imgCounter++;
                }
                if (keybindBarClasses != null) {
                    var listBarChange = keybindBarClasses.Where(p => p.key.ToLower().Equals(key.Key.ToString().ToLower()) && p.modifier.ToLower().Equals(modifier.ToLower()) && (p.bar.name.ToLower().Equals(style.ToLower()) || p.bar.name.Equals("ALL"))).Select(p => p).FirstOrDefault();
                    if (listBarChange != null) {
                        if (!listBarChange.name.ToLower().Equals("pause") && !listBarChange.name.ToLower().Equals("clear")) {
                            style = listBarChange.name;
                            TESTLABEL.Content = style;
                            changeStyle();
                        }
                    }
                }
                control = false;

            }

            #endregion
        }

        private bool abilCoolDown(List<Keypressed> prevKeys, Keypressed keypressed) {
            var prevKey = prevKeys.Where(pk => pk.ability.name == keypressed.ability.name).Select(pk => pk).FirstOrDefault();
            if (prevKey != null) {
                double abilCD = keypressed.ability.cooldown * 1000;
                if ((keypressed.timepressed - prevKey.timepressed) > abilCD) {
                    prevKeys.Remove(prevKey);
                    return false;
                }

                return true;
            } else {
                return false;
            }
        }


        private void moveImgs(int counter) {
            switch (counter) {
                case 1:
                    displayImg9.Source = displayImg10.Source;
                    break;
                case 2:
                    displayImg8.Source = displayImg9.Source;
                    displayImg9.Source = displayImg10.Source;
                    break;
                case 3:
                    displayImg7.Source = displayImg8.Source;
                    displayImg8.Source = displayImg9.Source;
                    displayImg9.Source = displayImg10.Source;
                    break;
                case 4:
                    displayImg6.Source = displayImg7.Source;
                    displayImg7.Source = displayImg8.Source;
                    displayImg8.Source = displayImg9.Source;
                    displayImg9.Source = displayImg10.Source;
                    break;
                case 5:
                    displayImg5.Source = displayImg6.Source;
                    displayImg6.Source = displayImg7.Source;
                    displayImg7.Source = displayImg8.Source;
                    displayImg8.Source = displayImg9.Source;
                    displayImg9.Source = displayImg10.Source;
                    break;
                case 6:
                    displayImg4.Source = displayImg5.Source;
                    displayImg5.Source = displayImg6.Source;
                    displayImg6.Source = displayImg7.Source;
                    displayImg7.Source = displayImg8.Source;
                    displayImg8.Source = displayImg9.Source;
                    displayImg9.Source = displayImg10.Source;
                    break;
                case 7:
                    displayImg3.Source = displayImg4.Source;
                    displayImg4.Source = displayImg5.Source;
                    displayImg5.Source = displayImg6.Source;
                    displayImg6.Source = displayImg7.Source;
                    displayImg7.Source = displayImg8.Source;
                    displayImg8.Source = displayImg9.Source;
                    displayImg9.Source = displayImg10.Source;
                    break;
                case 8:
                    displayImg2.Source = displayImg3.Source;
                    displayImg3.Source = displayImg4.Source;
                    displayImg4.Source = displayImg5.Source;
                    displayImg5.Source = displayImg6.Source;
                    displayImg6.Source = displayImg7.Source;
                    displayImg7.Source = displayImg8.Source;
                    displayImg8.Source = displayImg9.Source;
                    displayImg9.Source = displayImg10.Source;
                    break;
                case 9:
                    displayImg1.Source = displayImg2.Source;
                    displayImg2.Source = displayImg3.Source;
                    displayImg3.Source = displayImg4.Source;
                    displayImg4.Source = displayImg5.Source;
                    displayImg5.Source = displayImg6.Source;
                    displayImg6.Source = displayImg7.Source;
                    displayImg7.Source = displayImg8.Source;
                    displayImg8.Source = displayImg9.Source;
                    displayImg9.Source = displayImg10.Source;
                    break;
            }
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (e.ChangedButton == MouseButton.Left) {
                this.DragMove();
                //MainWindow.displayX = this.Top;
                //MainWindow.displayY = this.Left;
                //MainWindow.Height = this.Height;
                //MainWindow.Width = this.Width;
            }
        }
    }
}
