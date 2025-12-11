using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Updater
{
    public partial class UpdatePopupWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource;
        private string updateVersion;
        private bool isUpdateInProgress = false;

        public bool UpdateConfirmed { get; private set; } = false;
        public bool UpdateCancelled { get; private set; } = false;

        public UpdatePopupWindow(string version = "1.2.0")
        {
            InitializeComponent();
            updateVersion = version;
            UpdateVersionDisplay();

            // 창 닫기 이벤트 처리
            this.Closing += UpdatePopupWindow_Closing;
        }

        /// <summary>
        /// 창 닫기 이벤트 처리
        /// </summary>
        private void UpdatePopupWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 업데이트 진행 중일 때 - 메시지 없이 바로 취소 처리
            if (isUpdateInProgress)
            {
                // 업데이트 취소 처리
                if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                    UpdateCancelled = true;
                }
            }
        }

        /// <summary>
        /// 최신 버전 메시지 표시 (정적 메서드)
        /// </summary>
        public static void ShowAlreadyLatestVersion(Window owner = null)
        {
            MessageBox.Show(
                "이미 최신 버전입니다.\n업데이트가 필요하지 않습니다.",
                "최신 버전",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// 업데이트 확인 팝업 표시 (정적 메서드)
        /// </summary>
        public static bool? ShowUpdateConfirmation(string version, Window owner = null)
        {
            var popup = new UpdatePopupWindow(version)
            {
                Owner = owner,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };

            return popup.ShowDialog();
        }

        /// <summary>
        /// 버전 정보 표시 업데이트
        /// </summary>
        private void UpdateVersionDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                // 버전 텍스트 업데이트
                VersionText.Text = $"Ver {updateVersion}";
                Console.WriteLine($"[UpdatePopup] 버전 표시: Ver {updateVersion}");
            });
        }

        /// <summary>
        /// 제목 텍스트 변경 (업데이트 시작 시)
        /// </summary>
        private void UpdateTitleForProgress()
        {
            Dispatcher.Invoke(() =>
            {
                TitleSuffix.Text = ") 업데이트 중입니다.";
            });
        }

        /// <summary>
        /// 네 버튼 클릭 - 업데이트 시작
        /// </summary>
        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateConfirmed = true;
            StartUpdate();
        }

        /// <summary>
        /// 아니요 버튼 클릭 - 취소
        /// </summary>
        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateConfirmed = false;
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// 취소/닫기 버튼 클릭
        /// ⭐ 진행 중일 때 취소, 완료 후 닫기
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 업데이트 진행 중이면 취소 확인
            if (isUpdateInProgress && cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                var result = MessageBox.Show(
                    "업데이트를 취소하시겠습니까?",
                    "확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    cancellationTokenSource.Cancel();
                    UpdateCancelled = true;
                    this.DialogResult = false;
                    this.Close();
                }
            }
            else
            {
                // 업데이트 완료 후 - 바로 닫기
                Console.WriteLine("[UpdatePopup] 닫기 버튼 클릭 - 팝업 닫기");
                this.Close();
            }
        }

        /// <summary>
        /// 업데이트 시작
        /// </summary>
        private void StartUpdate()
        {
            // 업데이트 진행 상태 설정
            isUpdateInProgress = true;

            // UI 전환
            YesButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Visible;

            MessageText.Visibility = Visibility.Collapsed;
            SubMessageText.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;

            // 제목 텍스트 변경
            UpdateTitleForProgress();
            ProgressStatusText.Text = $"최신 버전(Ver {updateVersion}) 업데이트 중입니다.";

            // 실제 업데이트는 호출한 곳에서 진행
            UpdateConfirmed = true;
        }

        /// <summary>
        /// 진행률 업데이트 (외부에서 호출)
        /// </summary>
        public void UpdateProgress(int percentage, string message = null)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateProgressBar.Value = percentage;
                ProgressPercentText.Text = $"{percentage}%";

                // 진행바 Width 직접 계산 및 적용
                var progressBarFill = this.FindName("ProgressBarFill") as Border;
                var progressBarBackground = this.FindName("ProgressBarBackground") as Border;

                if (progressBarFill != null && progressBarBackground != null)
                {
                    // ActualWidth가 0이면 대기
                    if (progressBarBackground.ActualWidth > 0)
                    {
                        double targetWidth = (progressBarBackground.ActualWidth * percentage) / 100.0;
                        progressBarFill.Width = Math.Max(0, targetWidth);
                    }
                    else
                    {
                        // 레이아웃이 완료되지 않았을 때 대기 후 재시도
                        progressBarBackground.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (progressBarBackground.ActualWidth > 0)
                            {
                                double targetWidth = (progressBarBackground.ActualWidth * percentage) / 100.0;
                                progressBarFill.Width = Math.Max(0, targetWidth);
                            }
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }

                if (!string.IsNullOrEmpty(message))
                {
                    ProgressStatusText.Text = message;
                }

                Console.WriteLine($"[UpdatePopup] 진행률: {percentage}% - {message}");
            });
        }

        /// <summary>
        /// 업데이트 완료
        /// ⭐ Show()로 열린 창 닫기 + 버튼 텍스트 변경
        /// </summary>
        public void UpdateCompleted(bool success)
        {
            // 업데이트 진행 상태 해제
            isUpdateInProgress = false;

            Dispatcher.Invoke(() =>
            {
                if (success)
                {
                    TitleSuffix.Text = ") 업데이트가 완료되었습니다!";
                    ProgressStatusText.Text = "업데이트 성공";
                    UpdateProgressBar.Value = 100;
                    ProgressPercentText.Text = "100%";

                    // 진행바 100% 채우기
                    var progressBarFill = this.FindName("ProgressBarFill") as Border;
                    var progressBarBackground = this.FindName("ProgressBarBackground") as Border;
                    if (progressBarFill != null && progressBarBackground != null && progressBarBackground.ActualWidth > 0)
                    {
                        progressBarFill.Width = progressBarBackground.ActualWidth;
                    }

                    Console.WriteLine("[UpdatePopup] ✅ 업데이트 성공");
                }
                else
                {
                    TitleSuffix.Text = ") 업데이트에 실패했습니다.";
                    TitleSuffix.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x44, 0x44));
                    ProgressStatusText.Text = "업데이트 실패";
                    Console.WriteLine("[UpdatePopup] ❌ 업데이트 실패");
                }

                // ⭐ 취소 버튼을 닫기 버튼으로 변경
                Console.WriteLine("[UpdatePopup] 🔄 버튼 텍스트 변경 시작");
                var cancelButtonBorder = this.FindName("CancelButton") as Border;
                if (cancelButtonBorder != null)
                {
                    var textBlock = cancelButtonBorder.Child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.Text = "닫기";
                        Console.WriteLine("[UpdatePopup] ✅ 버튼 텍스트 변경 완료: 취소 → 닫기");
                    }
                    else
                    {
                        Console.WriteLine("[UpdatePopup] ⚠️ TextBlock을 찾을 수 없음");
                    }
                }
                else
                {
                    Console.WriteLine("[UpdatePopup] ⚠️ CancelButton을 찾을 수 없음");
                }

                // ⭐ 2초 후 자동 닫기 (Show()로 열린 창용)
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    Console.WriteLine("[UpdatePopup] ⏰ 2초 경과 - 창 닫기 시도");

                    try
                    {
                        // ⭐ 방법 1: Hide() + Sleep() + Close()
                        Console.WriteLine("[UpdatePopup] [1단계] Hide() 호출");
                        this.Hide();

                        Console.WriteLine("[UpdatePopup] [2단계] 100ms 대기");
                        System.Threading.Thread.Sleep(100);

                        Console.WriteLine("[UpdatePopup] [3단계] Close() 호출");
                        this.Close();

                        Console.WriteLine("[UpdatePopup] ✅ 창 닫기 성공!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[UpdatePopup] ❌ 방법1 실패: {ex.Message}");

                        // ⭐ 방법 2: 직접 Close()
                        try
                        {
                            Console.WriteLine("[UpdatePopup] [재시도] Close() 직접 호출");
                            this.Close();
                            Console.WriteLine("[UpdatePopup] ✅ (재시도) 창 닫기 성공!");
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"[UpdatePopup] ❌ (재시도) 실패: {ex2.Message}");
                        }
                    }
                };
                timer.Start();
                Console.WriteLine("[UpdatePopup] ⏱️ 타이머 시작 (2초 후 자동 닫기)");
            });
        }

        #region 버튼 마우스 오버 효과

        private void YesButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.2;
                AnimateBorderScale(border, 1.0, 1.05);
            }
        }

        private void YesButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.0;
                AnimateBorderScale(border, 1.05, 1.0);
            }
        }

        private void NoButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.2;
                AnimateBorderScale(border, 1.0, 1.05);
            }
        }

        private void NoButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.0;
                AnimateBorderScale(border, 1.05, 1.0);
            }
        }

        private void CancelButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.2;
                AnimateBorderScale(border, 1.0, 1.05);
            }
        }

        private void CancelButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.0;
                AnimateBorderScale(border, 1.05, 1.0);
            }
        }

        /// <summary>
        /// 테두리 확대/축소 애니메이션
        /// </summary>
        private void AnimateBorderScale(Border border, double from, double to)
        {
            var scaleTransform = border.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(from, from);
                border.RenderTransform = scaleTransform;
                border.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            var scaleXAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(150)
            };

            var scaleYAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(150)
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        #endregion
    }
}
