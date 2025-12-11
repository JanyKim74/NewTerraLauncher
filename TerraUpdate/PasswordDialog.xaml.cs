using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace Updater
{
    public partial class PasswordDialog : Window
    {
        private int attemptCount = 0;
        private const int MAX_ATTEMPTS = 3;

        public PasswordDialog()
        {
            InitializeComponent();

            // HiddenPasswordBox가 로드된 후 Focus 설정
            this.Loaded += (s, e) =>
            {
                if (HiddenPasswordBox != null)
                {
                    HiddenPasswordBox.Focus();
                }
            };
        }

        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ValidatePassword();
        }

        /// <summary>
        /// 비밀번호 검증 (adminConfig.json에서 읽기)
        /// </summary>
        private void ValidatePassword()
        {
            string enteredPassword = HiddenPasswordBox.Password;

            if (string.IsNullOrEmpty(enteredPassword))
            {
                ShowError("비밀번호를 입력하세요.");
                return;
            }

            // adminConfig.json에서 비밀번호 검증
            bool isValid = AdminConfigManager.ValidatePassword(enteredPassword);

            if (isValid)
            {
                Console.WriteLine("[PasswordDialog] 비밀번호 인증 성공");
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                attemptCount++;

                if (attemptCount >= MAX_ATTEMPTS)
                {
                    MessageBox.Show(
                        "비밀번호 입력 횟수를 초과했습니다.\n관리자에게 문의하세요.",
                        "인증 실패",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    Console.WriteLine($"[PasswordDialog] 비밀번호 인증 실패 - 최대 시도 횟수 초과");
                    this.DialogResult = false;
                    this.Close();
                }
                else
                {
                    int remainingAttempts = MAX_ATTEMPTS - attemptCount;
                    ShowError($"비밀번호가 올바르지 않습니다. (남은 횟수: {remainingAttempts}회)");
                    HiddenPasswordBox.Clear();
                    HiddenPasswordBox.Focus();

                    // 흔들기 애니메이션
                    ShakeAnimation();
                }

                Console.WriteLine($"[PasswordDialog] 비밀번호 인증 실패 ({attemptCount}/{MAX_ATTEMPTS})");
            }
        }

        /// <summary>
        /// 오류 메시지 표시
        /// </summary>
        private void ShowError(string message)
        {
            if (ErrorMessage != null)
            {
                ErrorMessage.Text = message;
                ErrorMessage.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 흔들기 애니메이션
        /// </summary>
        private void ShakeAnimation()
        {
            try
            {
                var storyboard = new Storyboard();
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 10,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(3)
                };

                Storyboard.SetTarget(animation, HiddenPasswordBox);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

                HiddenPasswordBox.RenderTransform = new System.Windows.Media.TranslateTransform();
                storyboard.Children.Add(animation);
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PasswordDialog] 애니메이션 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[PasswordDialog] 비밀번호 인증 취소");
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// 타이틀 바 드래그
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PasswordDialog] DragMove 오류: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 저장하기 버튼 클릭 (실제로는 확인 버튼)
        /// </summary>
        private void SaveButton_Click(object sender, MouseButtonEventArgs e)
        {
            ValidatePassword();
        }

        /// <summary>
        /// 저장하기 버튼 마우스 오버
        /// </summary>
        private void SaveButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.2;
                var scaleTransform = new ScaleTransform(1.0, 1.0);
                border.RenderTransform = scaleTransform;
                border.RenderTransformOrigin = new Point(0.5, 0.5);

                var animation = new DoubleAnimation
                {
                    To = 1.05,
                    Duration = TimeSpan.FromMilliseconds(150)
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            }
        }

        /// <summary>
        /// 저장하기 버튼 마우스 나감
        /// </summary>
        private void SaveButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.0;
                if (border.RenderTransform is ScaleTransform scaleTransform)
                {
                    var animation = new DoubleAnimation
                    {
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };

                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
                }
            }
        }

        /// <summary>
        /// HiddenPasswordBox 비밀번호 변경 이벤트
        /// </summary>
        private void HiddenPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // 5자리 입력되면 자동으로 검증
            if (HiddenPasswordBox.Password.Length == 5)
            {
                ValidatePassword();
            }

            // 비밀번호 입력 표시 업데이트
            UpdatePasswordDisplay();
        }

        /// <summary>
        /// 비밀번호 입력 표시 업데이트 (별표 표시)
        /// </summary>
        private void UpdatePasswordDisplay()
        {
            int length = HiddenPasswordBox.Password.Length;

            PasswordChar1.Visibility = length >= 1 ? Visibility.Visible : Visibility.Collapsed;
            PasswordChar2.Visibility = length >= 2 ? Visibility.Visible : Visibility.Collapsed;
            PasswordChar3.Visibility = length >= 3 ? Visibility.Visible : Visibility.Collapsed;
            PasswordChar4.Visibility = length >= 4 ? Visibility.Visible : Visibility.Collapsed;
            PasswordChar5.Visibility = length >= 5 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}