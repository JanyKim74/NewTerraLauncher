using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace Updater
{
    public partial class OptionsWindow : Window
    {
        private DefaultGameData gameData;      // ParkDay\Saved\defaultGameData.json
        private AdminConfig adminConfig;       // ParkDay\Saved\adminConfig.json

        // ⭐ 추가: 각 탭의 설정 로드 여부 플래그
        private bool isAdminSettingLoaded = false;
        private bool isGameSettingLoaded = false;
        private bool isSystemSettingLoaded = false;

        public OptionsWindow()
        {
            InitializeComponent();
            // Visual Tree가 완전히 로드된 후에 설정 로드
            this.Loaded += OptionsWindow_Loaded;
        }


        private void OptionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[OptionsWindow] Window Loaded 이벤트 발생");

            // ⭐ 관리자 설정 탭이 기본으로 열려있으므로 먼저 로드
            LoadAdminSettingsOnce();
        }

        #region 데이터 로드

        /// <summary>
        /// 모든 설정 로드
        /// </summary>
        private void LoadAllSettings()
        {
            try
            {
                // 1. 게임 옵션 로드 (defaultGameData.json)
                LoadGameOptions();

                // 2. 관리자 설정 로드 (adminConfig.json)
                LoadAdminSettings();

                // 3. 시스템 설정 로드 (adminConfig.json)
                LoadSystemSettings();

                Console.WriteLine("[OptionsWindow] 모든 설정 로드 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 로드 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"[OptionsWindow] 설정 로드 오류: {ex}");
            }
        }

        /// <summary>
        /// 게임 옵션 로드 (defaultGameData.json)
        /// </summary>
        private void LoadGameOptions()
        {
            try
            {
                gameData = GameOptionsManager.Load();
                var options = gameData.GameOptions;

                // 게임설정 탭
                SetRadioButton("Mulligan", options.MulliganCount);
                SetRadioButton("Concede", options.ConcedeDistance);
                SetRadioButton("Difficulty", options.Difficulty);
                SetRadioButton("PracticeTime", options.PracticeTimeLimit);
                SetRadioButton("PinPosition", options.PinPosition);

                Console.WriteLine("[OptionsWindow] 게임 옵션 로드 완료 (defaultGameData.json)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OptionsWindow] 게임 옵션 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 관리자 설정 로드 (adminConfig.json)
        /// </summary>
        private void LoadAdminSettings()
        {
            try
            {
                adminConfig = AdminConfigManager.Load();

                // 관리자 설정 탭
                PracticeTimeText.Text = adminConfig.PracticeTimeMinutes.ToString();

                Console.WriteLine("[OptionsWindow] 관리자 설정 로드 완료 (adminConfig.json)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OptionsWindow] 관리자 설정 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 시스템 설정 로드 (adminConfig.json)
        /// </summary>
        private void LoadSystemSettings()
        {
            try
            {
                if (adminConfig == null)
                {
                    adminConfig = AdminConfigManager.Load();
                }

                // 시스템 설정 탭
                DeviceIdText.Text = adminConfig.DeviceId;
                RoomNumberText.Text = adminConfig.RoomNumber;
                GazeControlText.Text = adminConfig.GazeControl.ToString();

                // 스윙모션
                SwingMotionUseRadio.IsChecked = adminConfig.SwingMotionEnabled;
                SwingMotionNotUseRadio.IsChecked = !adminConfig.SwingMotionEnabled;

                // 공 색상
                SetBallColor(adminConfig.BallColor);

                // 하드웨어 상태
                UpdateHardwareLEDs();

                Console.WriteLine("[OptionsWindow] 시스템 설정 로드 완료 (adminConfig.json)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OptionsWindow] 시스템 설정 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 공 색상 설정
        /// </summary>
        private void SetBallColor(string color)
        {
            switch (color)
            {
                case "White":
                    BallColor_White.IsChecked = true;
                    break;
                case "Yellow":
                    BallColor_Yellow.IsChecked = true;
                    break;
                case "Green":
                    BallColor_Green.IsChecked = true;
                    break;
                case "Blue":
                    BallColor_Blue.IsChecked = true;
                    break;
                case "Brown":
                default:
                    BallColor_Brown.IsChecked = true;
                    break;
            }
        }

        /// <summary>
        /// 선택된 공 색상 가져오기
        /// </summary>
        private string GetSelectedBallColor()
        {
            if (BallColor_White.IsChecked == true) return "White";
            if (BallColor_Yellow.IsChecked == true) return "Yellow";
            if (BallColor_Green.IsChecked == true) return "Green";
            if (BallColor_Blue.IsChecked == true) return "Blue";
            if (BallColor_Brown.IsChecked == true) return "Brown";
            return "Brown"; // 기본값
        }

        /// <summary>
        /// 하드웨어 LED 업데이트
        /// </summary>
        private void UpdateHardwareLEDs()
        {
            if (adminConfig?.HardwareStatus != null)
            {
                HW_MotionCAM_LED.Fill = new SolidColorBrush(
                    adminConfig.HardwareStatus.MotionCAM ? Colors.Green : Colors.Red);
                HW_AutoTee_LED.Fill = new SolidColorBrush(
                    adminConfig.HardwareStatus.AutoTee ? Colors.Green : Colors.Red);
                HW_Sensor_LED.Fill = new SolidColorBrush(
                    adminConfig.HardwareStatus.Sensor ? Colors.Green : Colors.Red);
                HW_Projector_LED.Fill = new SolidColorBrush(
                    adminConfig.HardwareStatus.Projector ? Colors.Green : Colors.Red);
                HW_Kiosk_LED.Fill = new SolidColorBrush(
                    adminConfig.HardwareStatus.Kiosk ? Colors.Green : Colors.Red);
            }
        }

        /// <summary>
        /// 라디오 버튼 설정
        /// </summary>
        private void SetRadioButton(string groupName, int value)
        {
            Console.WriteLine($"[SetRadioButton] 시작 - GroupName: {groupName}, Value: {value}");

            var radioButtons = FindVisualChildren<RadioButton>(this);
            int totalCount = 0;
            int matchCount = 0;

            foreach (var rb in radioButtons)
            {
                if (rb.GroupName == groupName)
                {
                    totalCount++;
                    Console.WriteLine($"  찾은 버튼: Name={rb.Name}, Tag={rb.Tag}, IsChecked={rb.IsChecked}");

                    if (rb.Tag != null)
                    {
                        int tagValue;
                        if (int.TryParse(rb.Tag.ToString(), out tagValue))
                        {
                            Console.WriteLine($"    Tag 파싱 성공: {tagValue}");
                            if (tagValue == value)
                            {
                                Console.WriteLine($"    ✓ 매칭! {rb.Name}을 체크합니다.");
                                rb.IsChecked = true;
                                matchCount++;
                                return;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"    Tag 파싱 실패: {rb.Tag}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"    Tag가 null입니다!");
                    }
                }
            }

            Console.WriteLine($"[SetRadioButton] 완료 - GroupName: {groupName}, 찾은 버튼 수: {totalCount}, 매칭된 버튼: {matchCount}");

            if (matchCount == 0)
            {
                Console.WriteLine($"[SetRadioButton] 경고: {groupName}={value}에 해당하는 버튼을 찾지 못했습니다!");
            }
        }

        /// <summary>
        /// 라디오 버튼 값 가져오기
        /// </summary>
        private int GetRadioButtonValue(string groupName)
        {
            var radioButtons = FindVisualChildren<RadioButton>(this);
            
            foreach (var rb in radioButtons)
            {
                if (rb.GroupName == groupName && rb.IsChecked == true && rb.Tag != null)
                {
                    int value;
                    if (int.TryParse(rb.Tag.ToString(), out value))
                    {
                        return value;
                    }
                }
            }
            
            return 0;
        }

        /// <summary>
        /// Visual Tree 검색
        /// </summary>
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        #endregion

        #region 관리자 설정 탭

        /// <summary>
        /// 연습장 시간 감소
        /// </summary>
        private void DecreasePracticeTime_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PracticeTimeText.Text, out int currentTime))
            {
                if (currentTime > 1)
                {
                    PracticeTimeText.Text = (currentTime - 1).ToString();
                }
            }
        }

        /// <summary>
        /// 연습장 시간 증가
        /// </summary>
        private void IncreasePracticeTime_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PracticeTimeText.Text, out int currentTime))
            {
                if (currentTime < 999)
                {
                    PracticeTimeText.Text = (currentTime + 1).ToString();
                }
            }
        }

        #endregion

        #region 시스템 설정 탭

        /// <summary>
        /// 시선 조절 감소
        /// </summary>
        private void DecreaseGazeControl_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(GazeControlText.Text, out int currentValue))
            {
                if (currentValue > -10)
                {
                    GazeControlText.Text = (currentValue - 1).ToString();
                }
            }
        }

        /// <summary>
        /// 시선 조절 증가
        /// </summary>
        private void IncreaseGazeControl_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(GazeControlText.Text, out int currentValue))
            {
                if (currentValue < 10)
                {
                    GazeControlText.Text = (currentValue + 1).ToString();
                }
            }
        }

        #endregion

        #region 탭 전환

        private void AdminSettingTab_Click(object sender, MouseButtonEventArgs e)
        {
            AdminSettingContent.Visibility = Visibility.Visible;
            GameSettingContent.Visibility = Visibility.Collapsed;
            SystemSettingContent.Visibility = Visibility.Collapsed;

            // 탭 헤더 이미지 변경
            AdminSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_on.png")));
            GameSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));
            SystemSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));

            // 텍스트 색상 변경
            var adminText = AdminSettingHeader.Child as TextBlock;
            if (adminText != null) adminText.Foreground = new SolidColorBrush(Color.FromRgb(0xF3, 0xFD, 0xFF));
            
            GameSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));
            SystemSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));

            // 관리자 설정 로드
            LoadAdminSettingsOnce();
        }

        private void GameSettingTab_Click(object sender, MouseButtonEventArgs e)
        {
            AdminSettingContent.Visibility = Visibility.Collapsed;
            GameSettingContent.Visibility = Visibility.Visible;
            this.UpdateLayout();
            SystemSettingContent.Visibility = Visibility.Collapsed;

            // 탭 헤더 이미지 변경
            AdminSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));
            GameSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_on.png")));
            SystemSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));

            // 텍스트 색상 변경
            var adminText = AdminSettingHeader.Child as TextBlock;
            if (adminText != null) adminText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));
            
            GameSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0xF3, 0xFD, 0xFF));
            SystemSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));

            // 게임 설정 로드
            LoadGameSettingsOnce();
        }

        private void SystemSettingTab_Click(object sender, MouseButtonEventArgs e)
        {
            AdminSettingContent.Visibility = Visibility.Collapsed;
            GameSettingContent.Visibility = Visibility.Collapsed;
            SystemSettingContent.Visibility = Visibility.Visible;

            // 탭 헤더 이미지 변경
            AdminSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));
            GameSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));
            SystemSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_on.png")));

            // 텍스트 색상 변경
            var adminText = AdminSettingHeader.Child as TextBlock;
            if (adminText != null) adminText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));
            
            GameSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));
            SystemSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0xF3, 0xFD, 0xFF));

            // 시스템 설정 로드
            LoadSystemSettingsOnce();
        }

        #endregion

        #region 저장

        private void SaveButton_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Console.WriteLine("════════════════════════════════════════════════════════");
                Console.WriteLine("[저장 시작]");
                Console.WriteLine("════════════════════════════════════════════════════════");

                // 1. 관리자 설정 저장 (adminConfig.json)
                SaveAdminSettings();

                // 2. 게임 옵션 저장 (defaultGameData.json)
                SaveGameOptions();

                // 3. 시스템 설정 저장 (adminConfig.json)
                SaveSystemSettings();

                Console.WriteLine("════════════════════════════════════════════════════════");
                Console.WriteLine("[저장 완료]");
                Console.WriteLine("════════════════════════════════════════════════════════");

                MessageBox.Show("모든 설정이 저장되었습니다.", "저장 완료",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"[OptionsWindow] 저장 오류: {ex}");
            }
        }

        /// <summary>
        /// 관리자 설정 저장 (adminConfig.json)
        /// </summary>
        private void SaveAdminSettings()
        {
            Console.WriteLine("[관리자 설정 저장 시작]");

            // 비밀번호 변경 확인
            if (!string.IsNullOrEmpty(OldPasswordBox.Password))
            {
                if (!ValidatePasswordChange())
                {
                    throw new Exception("비밀번호 변경 검증 실패");
                }
            }

            // 연습장 시간
            if (int.TryParse(PracticeTimeText.Text, out int practiceTime))
            {
                adminConfig.PracticeTimeMinutes = practiceTime;
                Console.WriteLine($"  연습장 시간: {practiceTime}분");
            }

            AdminConfigManager.Save(adminConfig);
            Console.WriteLine("[관리자 설정 저장 완료] → adminConfig.json");
        }

        /// <summary>
        /// 게임 옵션 저장 (defaultGameData.json)
        /// </summary>
        private void SaveGameOptions()
        {
            Console.WriteLine("[게임 옵션 저장 시작]");

            var options = gameData.GameOptions;
            options.MulliganCount = GetRadioButtonValue("Mulligan");
            options.ConcedeDistance = GetRadioButtonValue("Concede");
            options.Difficulty = GetRadioButtonValue("Difficulty");
            options.PracticeTimeLimit = GetRadioButtonValue("PracticeTime");
            options.PinPosition = GetRadioButtonValue("PinPosition");

            Console.WriteLine($"  멀리건: {options.MulliganCount}");
            Console.WriteLine($"  컨시드: {options.ConcedeDistance}");
            Console.WriteLine($"  난이도: {options.Difficulty}");
            Console.WriteLine($"  연습장 시간제한: {options.PracticeTimeLimit}");
            Console.WriteLine($"  핀위치: {options.PinPosition}");

            GameOptionsManager.Save(gameData);
            Console.WriteLine("[게임 옵션 저장 완료] → defaultGameData.json");
        }

        /// <summary>
        /// 시스템 설정 저장 (adminConfig.json)
        /// </summary>
        private void SaveSystemSettings()
        {
            Console.WriteLine("[시스템 설정 저장 시작]");

            adminConfig.DeviceId = DeviceIdText.Text;
            adminConfig.RoomNumber = RoomNumberText.Text;
            adminConfig.SwingMotionEnabled = SwingMotionUseRadio.IsChecked == true;
            
            if (int.TryParse(GazeControlText.Text, out int gazeControl))
            {
                adminConfig.GazeControl = gazeControl;
            }

            adminConfig.BallColor = GetSelectedBallColor();

            Console.WriteLine($"  장비 ID: {adminConfig.DeviceId}");
            Console.WriteLine($"  룸 번호: {adminConfig.RoomNumber}");
            Console.WriteLine($"  스윙모션: {(adminConfig.SwingMotionEnabled ? "사용" : "미사용")}");
            Console.WriteLine($"  시선 조절: {adminConfig.GazeControl}");
            Console.WriteLine($"  공 색상: {adminConfig.BallColor}");

            AdminConfigManager.Save(adminConfig);
            Console.WriteLine("[시스템 설정 저장 완료] → adminConfig.json");
        }

        /// <summary>
        /// 비밀번호 변경 검증
        /// </summary>
        private bool ValidatePasswordChange()
        {
            string oldPassword = OldPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            Console.WriteLine("[비밀번호 변경 검증]");

            // 이전 비밀번호 확인
            if (adminConfig.AdminPassword != oldPassword)
            {
                MessageBox.Show("이전 비밀번호가 올바르지 않습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine("  실패: 이전 비밀번호 불일치");
                return false;
            }

            // 새 비밀번호 길이 확인
            if (newPassword.Length != 5)
            {
                MessageBox.Show("새 비밀번호는 5자리여야 합니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine("  실패: 새 비밀번호 길이 오류");
                return false;
            }

            // 비밀번호 일치 확인
            if (newPassword != confirmPassword)
            {
                MessageBox.Show("새 비밀번호가 일치하지 않습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine("  실패: 새 비밀번호 불일치");
                return false;
            }

            // 비밀번호 변경
            adminConfig.AdminPassword = newPassword;
            Console.WriteLine($"  성공: 비밀번호 변경됨 ({new string('*', 5)})");
            
            MessageBox.Show("비밀번호가 변경되었습니다.", "성공",
                MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        #endregion

        #region UI 효과

        private void SaveButton_MouseEnter(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
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

        private void SaveButton_MouseLeave(object sender, MouseEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
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

        #endregion

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        /// <summary>
        /// 관리자 설정 로드 (한 번만)
        /// </summary>
        private void LoadAdminSettingsOnce()
        {
            if (isAdminSettingLoaded) return;

            Console.WriteLine("[OptionsWindow] 관리자 설정 로드 시작");
            LoadAdminSettings();
            isAdminSettingLoaded = true;
        }

        /// <summary>
        /// 게임 설정 로드 (한 번만)
        /// </summary>
        private void LoadGameSettingsOnce()
        {
            if (isGameSettingLoaded) return;

            Console.WriteLine("[OptionsWindow] 게임 설정 로드 시작");
            LoadGameOptions();
            isGameSettingLoaded = true;
        }

        /// <summary>
        /// 시스템 설정 로드 (한 번만)
        /// </summary>
        private void LoadSystemSettingsOnce()
        {
            if (isSystemSettingLoaded) return;

            Console.WriteLine("[OptionsWindow] 시스템 설정 로드 시작");
            LoadSystemSettings();
            isSystemSettingLoaded = true;
        }
    }
}
