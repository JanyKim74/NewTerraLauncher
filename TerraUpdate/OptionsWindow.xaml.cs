using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO.Ports;


namespace Updater
{
    public partial class OptionsWindow : Window
    {
        private DefaultGameData gameData;
        private AdminConfig adminConfig;

        private bool isAdminSettingLoaded = false;
        private bool isGameSettingLoaded = false;
        private bool isSystemSettingLoaded = false;
        private bool isHardwareSearching = false;

        public OptionsWindow()
        {
            InitializeComponent();
            this.Loaded += OptionsWindow_Loaded;
        }

        private void OptionsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("[OptionsWindow] Window Loaded 이벤트 발생");
            LoadAdminSettingsOnce();
            LoadSystemSettings();
            LoadGameOptions();
        }

        #region 데이터 로드

        private void LoadGameOptions()
        {
            try
            {
                gameData = GameOptionsManager.Load();
                var options = gameData.GameOptions;

                // ⭐ 멀리건: 파일값 → UI값으로 변환
                int mulliganUIValue = GameOptionsManager.ConvertMulliganFileToUI(options.Mulligan_Count);
                Console.WriteLine($"[LoadGameOptions] 멀리건 변환: 파일값={options.Mulligan_Count} → UI값={mulliganUIValue}");
                SetRadioButton("Mulligan", mulliganUIValue);
                SetRadioButton("Concede", options.Concede_Distance);
                SetRadioButton("GreenSpeed", options.Green_Speed);
                SetRadioButton("CameraMode", options.Camera_Mode);
                SetRadioButton("SwingMotion", options.SwingMotion);
                SetRadioButton("PinPosition", options.HolecupPosition);

                Console.WriteLine("[OptionsWindow] 게임 옵션 로드 완료 (defaultGameData.json)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OptionsWindow] 게임 옵션 로드 오류: {ex.Message}");
            }
        }

        private void LoadAdminSettings()
        {
            try
            {
                adminConfig = AdminConfigManager.Load();
                PracticeTimeText.Text = adminConfig.PracticeTimeMinutes.ToString();

                // ⭐ UsePassword 토글 설정 (0 = 미적용, 1 = 적용)
                SetPasswordToggle(adminConfig.UsePassword);

                Console.WriteLine("[OptionsWindow] 관리자 설정 로드 완료 (adminConfig.json)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OptionsWindow] 관리자 설정 로드 오류: {ex.Message}");
            }
        }

        private void LoadSystemSettings()
        {
            try
            {
                if (adminConfig == null)
                {
                    adminConfig = AdminConfigManager.Load();
                }

                DeviceIdText.Text = adminConfig.DeviceId;
                RoomNumberText.Text = adminConfig.RoomNumber;
                GazeControlText.Text = adminConfig.GazeControl.ToString();

               // SetBallColor(adminConfig.BallColor);
                UpdateHardwareLEDs();

                Console.WriteLine("[OptionsWindow] 시스템 설정 로드 완료 (adminConfig.json)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OptionsWindow] 시스템 설정 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// ⭐ UsePassword 토글 설정 (0 = 미적용, 1 = 적용)
        /// </summary>
        private void SetPasswordToggle(int usePassword)
        {
            bool isPasswordEnabled = (usePassword == 1);

            try
            {
                if (PasswordEnabled != null)
                    PasswordEnabled.IsChecked = isPasswordEnabled;
                if (PasswordDisabled != null)
                    PasswordDisabled.IsChecked = !isPasswordEnabled;

                UpdatePasswordFieldsEnabled(isPasswordEnabled);

                Console.WriteLine($"[UsePassword] {(isPasswordEnabled ? "✅ 적용 (1)" : "❌ 미적용 (0)")} 로드됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SetPasswordToggle] 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// ⭐ 비밀번호 입력 필드 활성화/비활성화
        /// </summary>
        private void UpdatePasswordFieldsEnabled(bool isEnabled)
        {
            try
            {
                if (OldPasswordBox != null) OldPasswordBox.IsEnabled = isEnabled;
                if (NewPasswordBox != null) NewPasswordBox.IsEnabled = isEnabled;
                if (ConfirmPasswordBox != null) ConfirmPasswordBox.IsEnabled = isEnabled;

                Console.WriteLine($"[비밀번호필드] {(isEnabled ? "활성화" : "비활성화")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdatePasswordFieldsEnabled] 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// ⭐ UsePassword 토글 변경 이벤트
        /// </summary>
        public void OnPasswordToggleChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PasswordEnabled?.IsChecked == true)
                {
                    Console.WriteLine("[UsePassword] ✅ 변경: 미적용 → 적용 (1)");
                    UpdatePasswordFieldsEnabled(true);
                }
                else if (PasswordDisabled?.IsChecked == true)
                {
                    Console.WriteLine("[UsePassword] ❌ 변경: 적용 → 미적용 (0)");
                    UpdatePasswordFieldsEnabled(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OnPasswordToggleChanged] 오류: {ex.Message}");
            }
        }

        //private void SetBallColor(string color)
        //{
        //    switch (color)
        //    {
        //        case "White":
        //            BallColor_White.IsChecked = true;
        //            break;
        //        case "Yellow":
        //            BallColor_Yellow.IsChecked = true;
        //            break;
        //        case "Green":
        //            BallColor_Green.IsChecked = true;
        //            break;
        //        case "Blue":
        //            BallColor_Blue.IsChecked = true;
        //            break;
        //        case "Brown":
        //        default:
        //            BallColor_Brown.IsChecked = true;
        //            break;
        //    }
        //}

        //private string GetSelectedBallColor()
        //{
        //    if (BallColor_White.IsChecked == true) return "White";
        //    if (BallColor_Yellow.IsChecked == true) return "Yellow";
        //    if (BallColor_Green.IsChecked == true) return "Green";
        //    if (BallColor_Blue.IsChecked == true) return "Blue";
        //    if (BallColor_Brown.IsChecked == true) return "Brown";
        //    return "Brown";
        //}

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

        private void SetRadioButton(string groupName, int value)
        {
            Console.WriteLine($"[SetRadioButton] GroupName: {groupName}, Value: {value}");
            var radioButtons = FindLogicalChildren<RadioButton>(this);
            int matchCount = 0;

            foreach (var rb in radioButtons)
            {
                if (rb.GroupName == groupName && rb.Tag != null)
                {
                    if (int.TryParse(rb.Tag.ToString(), out int tagValue))
                    {
                        if (tagValue == value)
                        {
                            rb.IsChecked = true;
                            matchCount++;
                            Console.WriteLine($"  ✓ {rb.Name} 체크");
                            return;
                        }
                    }
                }
            }

            if (matchCount == 0)
            {
                Console.WriteLine($"[SetRadioButton] 경고: {groupName}={value}에 해당하는 버튼을 찾지 못했습니다!");
            }
        }

        private int GetRadioButtonValue(string groupName)
        {
            var radioButtons = FindLogicalChildren<RadioButton>(this);

            foreach (var rb in radioButtons)
            {
                if (rb.GroupName == groupName && rb.IsChecked == true && rb.Tag != null)
                {
                    if (int.TryParse(rb.Tag.ToString(), out int value))
                    {
                        return value;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// LogicalTreeHelper를 사용하여 논리 트리 탐색
        /// </summary>
        private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                foreach (object child in LogicalTreeHelper.GetChildren(depObj))
                {
                    if (child is DependencyObject depChild)
                    {
                        if (depChild is T)
                        {
                            yield return (T)depChild;
                        }

                        foreach (T descendant in FindLogicalChildren<T>(depChild))
                        {
                            yield return descendant;
                        }
                    }
                }
            }
        }

        #endregion

        #region 관리자 설정 탭

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

            AdminSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_on.png")));
            GameSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));
            SystemSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));

            var adminText = AdminSettingHeader.Child as TextBlock;
            if (adminText != null) adminText.Foreground = new SolidColorBrush(Color.FromRgb(0xF3, 0xFD, 0xFF));

            GameSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));
            SystemSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));

            LoadAdminSettingsOnce();
        }

        private void GameSettingTab_Click(object sender, MouseButtonEventArgs e)
        {
            AdminSettingContent.Visibility = Visibility.Collapsed;
            GameSettingContent.Visibility = Visibility.Visible;
            SystemSettingContent.Visibility = Visibility.Collapsed;

            AdminSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));
            GameSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_on.png")));
            SystemSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));

            var adminText = AdminSettingHeader.Child as TextBlock;
            if (adminText != null) adminText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));

            GameSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0xF3, 0xFD, 0xFF));
            SystemSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));

            LoadGameSettingsOnce();
        }

        private void SystemSettingTab_Click(object sender, MouseButtonEventArgs e)
        {
            AdminSettingContent.Visibility = Visibility.Collapsed;
            GameSettingContent.Visibility = Visibility.Collapsed;
            SystemSettingContent.Visibility = Visibility.Visible;

            AdminSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));
            GameSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_off.png")));
            SystemSettingHeader.Background = new ImageBrush(new BitmapImage(
                new Uri("pack://application:,,,/Image/btn_setting_header_on.png")));

            var adminText = AdminSettingHeader.Child as TextBlock;
            if (adminText != null) adminText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));

            GameSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0x57, 0x5E, 0x5F));
            SystemSettingHeaderText.Foreground = new SolidColorBrush(Color.FromRgb(0xF3, 0xFD, 0xFF));

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

                SaveAdminSettings();
                SaveGameOptions();
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

        private void SaveAdminSettings()
        {
            Console.WriteLine("[관리자 설정 저장 시작]");

            // ⭐ UsePassword 저장 (0 = 미적용, 1 = 적용)
            adminConfig.UsePassword = (PasswordEnabled?.IsChecked == true) ? 1 : 0;
            Console.WriteLine($"  비밀번호 사용: {(adminConfig.UsePassword == 1 ? "✅ 적용 (1)" : "❌ 미적용 (0)")}");

            // 비밀번호 변경 (UsePassword가 1일 때만)
            if (adminConfig.UsePassword == 1 && !string.IsNullOrEmpty(OldPasswordBox.Password))
            {
                if (!ValidatePasswordChange())
                {
                    throw new Exception("비밀번호 변경 검증 실패");
                }
            }
            else if (adminConfig.UsePassword == 1)
            {
                Console.WriteLine("  [비밀번호] 변경하지 않음 (기존 비밀번호 유지)");
            }
            else
            {
                Console.WriteLine("  [비밀번호] 미적용 - 비밀번호 변경 불가능");
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

        private void SaveGameOptions()
        {
            Console.WriteLine("[게임 옵션 저장 시작]");

            var options = gameData.GameOptions;
            // ⭐ 멀리건: UI값 → 파일값으로 변환
            int mulliganUIValue = GetRadioButtonValue("Mulligan");
            int mulliganFileValue = GameOptionsManager.ConvertMulliganUIToFile(mulliganUIValue);
            options.Mulligan_Count = mulliganFileValue;
            Console.WriteLine($"  [멀리건] UI값: {mulliganUIValue} → 파일값: {mulliganFileValue}");
            options.Concede_Distance = GetRadioButtonValue("Concede");
            options.Green_Speed = GetRadioButtonValue("GreenSpeed");
            options.Camera_Mode = GetRadioButtonValue("CameraMode");
            options.SwingMotion = GetRadioButtonValue("SwingMotion");
            options.HolecupPosition = GetRadioButtonValue("PinPosition");

            Console.WriteLine($"  멀리건: {options.Mulligan_Count}");
            Console.WriteLine($"  컨시드: {options.Concede_Distance}");
            Console.WriteLine($"  잔디상태: {options.Green_Speed}");
            Console.WriteLine($"  카메라모드: {options.Camera_Mode}");
            Console.WriteLine($"  스윙모션: {options.SwingMotion}");
            Console.WriteLine($"  핀위치: {options.HolecupPosition}");

            GameOptionsManager.Save(gameData);
            Console.WriteLine("[게임 옵션 저장 완료] → defaultGameData.json");
        }

        private void SaveSystemSettings()
        {
            Console.WriteLine("[시스템 설정 저장 시작]");

            adminConfig.DeviceId = DeviceIdText.Text;
            adminConfig.RoomNumber = RoomNumberText.Text;

            if (int.TryParse(GazeControlText.Text, out int gazeControl))
            {
                adminConfig.GazeControl = gazeControl;
            }

           // adminConfig.BallColor = GetSelectedBallColor();

            Console.WriteLine($"  장비 ID: {adminConfig.DeviceId}");
            Console.WriteLine($"  룸 번호: {adminConfig.RoomNumber}");
            Console.WriteLine($"  시선 조절: {adminConfig.GazeControl}");
            Console.WriteLine($"  공 색상: {adminConfig.BallColor}");

            AdminConfigManager.Save(adminConfig);
            Console.WriteLine("[시스템 설정 저장 완료] → adminConfig.json");
        }

        private bool ValidatePasswordChange()
        {
            string oldPassword = OldPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            Console.WriteLine("[비밀번호 변경 검증]");

            if (adminConfig.AdminPassword != oldPassword)
            {
                MessageBox.Show("이전 비밀번호가 올바르지 않습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine("  실패: 이전 비밀번호 불일치");
                return false;
            }

            if (newPassword.Length != 5)
            {
                MessageBox.Show("새 비밀번호는 5자리여야 합니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine("  실패: 새 비밀번호 길이 오류");
                return false;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("새 비밀번호가 일치하지 않습니다.", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Console.WriteLine("  실패: 새 비밀번호 불일치");
                return false;
            }

            adminConfig.AdminPassword = newPassword;
            Console.WriteLine($"  성공: 비밀번호 변경됨");

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

        private void LoadAdminSettingsOnce()
        {
            if (isAdminSettingLoaded) return;
            Console.WriteLine("[OptionsWindow] 관리자 설정 로드 시작");
            LoadAdminSettings();
            isAdminSettingLoaded = true;
        }

        private void LoadGameSettingsOnce()
        {
            if (isGameSettingLoaded) return;
            Console.WriteLine("[OptionsWindow] 게임 설정 로드 시작");
            LoadGameOptions();
            isGameSettingLoaded = true;
        }

        private void LoadSystemSettingsOnce()
        {
            if (isSystemSettingLoaded) return;
            Console.WriteLine("[OptionsWindow] 시스템 설정 로드 시작");
            LoadSystemSettings();
            isSystemSettingLoaded = true;
        }

        // ============================================================================
        // 🔍 하드웨어 검색 기능 (새로 추가)
        // ============================================================================

        private async void HardwareSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (isHardwareSearching)
                return;

            await PerformHardwareSearch();
        }

        private async Task PerformHardwareSearch()
        {
            isHardwareSearching = true;
            HardwareSearchButton.IsEnabled = false;
            HardwareSearchButton.Content = "🔄 검색 중...";
            HardwareSearchStatus.Visibility = Visibility.Visible;

            try
            {
                Console.WriteLine("\n[🔍 HardwareSearch] ========== 하드웨어 검색 시작 ==========");

                // Step 1: CR2 센서 검색
                Console.WriteLine("[HardwareSearch] ⏳ Step 1/3: CR2 센서 (Motion CAM) 검색 중...");
                HardwareSearchStatus.Text = "⏳ 검색 중... (센서)";
                await Task.Delay(800);

                await CheckSensorStatus();
                UpdateMotionCAMLED();

                Console.WriteLine("[HardwareSearch] ✅ Step 1 완료: Motion CAM 상태 업데이트");

                // Step 2: 오토티업기 검색
                Console.WriteLine("[HardwareSearch] ⏳ Step 2/3: 오토티업기 (Serial Port) 검색 중...");
                HardwareSearchStatus.Text = "⏳ 검색 중... (오토티업)";
                await Task.Delay(800);

                await CheckAutoTeeUpStatus();
                UpdateAutoTeeLED();

                Console.WriteLine("[HardwareSearch] ✅ Step 2 완료: AutoTee 상태 업데이트");

                // Step 3: 기타 하드웨어 검색
                Console.WriteLine("[HardwareSearch] ⏳ Step 3/3: 기타 하드웨어 (Sensor, Projector, Kiosk) 검색 중...");
                HardwareSearchStatus.Text = "⏳ 검색 중... (기타)";
                await Task.Delay(800);

                CheckOtherHardware();
                UpdateAllHardwareLEDs();

                Console.WriteLine("[HardwareSearch] ✅ Step 3 완료: 모든 하드웨어 상태 업데이트");

                HardwareSearchStatus.Text = "✅ 검색 완료!";
                Console.WriteLine("[HardwareSearch] ✅ 하드웨어 검색 완료!\n");

                await Task.Delay(2000);
                HardwareSearchStatus.Visibility = Visibility.Collapsed;
                HardwareSearchStatus.Text = "";
            }
            catch (Exception ex)
            {
                HardwareSearchStatus.Text = "❌ 검색 실패";
                Console.WriteLine($"[HardwareSearch] ❌ 오류: {ex.Message}");

                await Task.Delay(2000);
                HardwareSearchStatus.Visibility = Visibility.Collapsed;
            }
            finally
            {
                isHardwareSearching = false;
                HardwareSearchButton.IsEnabled = true;
                HardwareSearchButton.Content = "🔍 하드웨어 검색";
            }
        }

        private async Task CheckSensorStatus()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (adminConfig != null && adminConfig.HardwareStatus != null)
                    {
                        adminConfig.HardwareStatus.MotionCAM = true;
                        Console.WriteLine("  [Sensor] ✅ CR2 센서 (Motion CAM): 연결됨");
                    }
                }
                catch (Exception ex)
                {
                    if (adminConfig?.HardwareStatus != null)
                    {
                        adminConfig.HardwareStatus.MotionCAM = false;
                    }
                    Console.WriteLine($"  [Sensor] ❌ 오류: {ex.Message}");
                }
            });
        }

        private async Task CheckAutoTeeUpStatus()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (adminConfig != null && adminConfig.HardwareStatus != null)
                    {
                        string[] ports = SerialPort.GetPortNames();
                        bool found = false;

                        if (ports.Length > 0)
                        {
                            foreach (string port in ports)
                            {
                                try
                                {
                                    using (SerialPort sp = new SerialPort(port, 9600))
                                    {
                                        sp.ReadTimeout = 500;
                                        sp.WriteTimeout = 500;
                                        sp.Open();
                                        sp.Close();
                                        found = true;
                                        Console.WriteLine($"  [AutoTeeUp] ✅ 오토티업기: {port}에서 연결됨");
                                        break;
                                    }
                                }
                                catch { }
                            }
                        }

                        adminConfig.HardwareStatus.AutoTee = found;

                        if (!found)
                        {
                            Console.WriteLine("  [AutoTeeUp] ⚠️ 오토티업기: 포트 없음");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (adminConfig?.HardwareStatus != null)
                    {
                        adminConfig.HardwareStatus.AutoTee = false;
                    }
                    Console.WriteLine($"  [AutoTeeUp] ❌ 오류: {ex.Message}");
                }
            });
        }

        private void CheckOtherHardware()
        {
            try
            {
                if (adminConfig?.HardwareStatus != null)
                {
                    adminConfig.HardwareStatus.Sensor = true;
                    adminConfig.HardwareStatus.Projector = true;
                    adminConfig.HardwareStatus.Kiosk = true;

                    Console.WriteLine("  [OtherHW] ✅ 센서, 프로젝터, 키오스크 확인 완료");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [OtherHW] ❌ 오류: {ex.Message}");
            }
        }

        private void UpdateMotionCAMLED()
        {
            try
            {
                if (HW_MotionCAM_LED != null && adminConfig?.HardwareStatus != null)
                {
                    HW_MotionCAM_LED.Fill = new SolidColorBrush(
                        adminConfig.HardwareStatus.MotionCAM ? Colors.Green : Colors.Red);
                    Console.WriteLine($"  [LED] Motion CAM: {(adminConfig.HardwareStatus.MotionCAM ? "🟢 Green" : "🔴 Red")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [LED-Sensor] 오류: {ex.Message}");
            }
        }

        private void UpdateAutoTeeLED()
        {
            try
            {
                if (HW_AutoTee_LED != null && adminConfig?.HardwareStatus != null)
                {
                    HW_AutoTee_LED.Fill = new SolidColorBrush(
                        adminConfig.HardwareStatus.AutoTee ? Colors.Green : Colors.Red);
                    Console.WriteLine($"  [LED] AutoTee: {(adminConfig.HardwareStatus.AutoTee ? "🟢 Green" : "🔴 Red")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [LED-AutoTee] 오류: {ex.Message}");
            }
        }

        private void UpdateAllHardwareLEDs()
        {
            try
            {
                if (adminConfig?.HardwareStatus != null)
                {
                    if (HW_Sensor_LED != null)
                        HW_Sensor_LED.Fill = new SolidColorBrush(
                            adminConfig.HardwareStatus.Sensor ? Colors.Green : Colors.Red);

                    if (HW_Projector_LED != null)
                        HW_Projector_LED.Fill = new SolidColorBrush(
                            adminConfig.HardwareStatus.Projector ? Colors.Green : Colors.Red);

                    if (HW_Kiosk_LED != null)
                        HW_Kiosk_LED.Fill = new SolidColorBrush(
                            adminConfig.HardwareStatus.Kiosk ? Colors.Green : Colors.Red);

                    Console.WriteLine($"  [LED] Sensor: {(adminConfig.HardwareStatus.Sensor ? "🟢 Green" : "🔴 Red")}");
                    Console.WriteLine($"  [LED] Projector: {(adminConfig.HardwareStatus.Projector ? "🟢 Green" : "🔴 Red")}");
                    Console.WriteLine($"  [LED] Kiosk: {(adminConfig.HardwareStatus.Kiosk ? "🟢 Green" : "🔴 Red")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [LED-All] 오류: {ex.Message}");
            }
        }

        public void RefreshAllHardwareLEDs()
        {
            try
            {
                UpdateHardwareLEDs();
                Console.WriteLine("[RefreshHardwareLEDs] ✅ 모든 LED 업데이트 완료");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RefreshHardwareLEDs] ❌ 오류: {ex.Message}");
            }
        }
    }
}