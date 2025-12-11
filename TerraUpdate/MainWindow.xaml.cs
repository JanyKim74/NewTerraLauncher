using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;  // ⭐ 추가
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;  // ⭐ 여기에 추가!


using Microsoft.Win32;
using System.Collections.Generic;

namespace Updater
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient = new HttpClient();
        private UpdateManager updateManager;
        private LauncherConfig launcherConfig;
        private CancellationTokenSource updateCancellationTokenSource;

        private bool sensorConnected = false;
        private bool serialConnected = false;
        private bool gameCanStart = false;

#pragma warning disable CS0649
        private GameUpdateInfo currentUpdateInfo;
#pragma warning restore CS0649

        private GameUpdateInfoList currentUpdateInfoList;  // ⭐ 추가: 배열 형태의 업데이트 정보

        // CR2 XCAM 센서 DLL 함수 선언
        [DllImport("XcamAdapt64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr CR2_init(uint sensorcode, uint sensornum, long p0, long p1, long p2, long p3);

        [DllImport("XcamAdapt64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CR2_delete(IntPtr handle);

        private IntPtr sensorHandle = IntPtr.Zero;

        public MainWindow()
        {
            InitializeComponent();

            // UpdateManager 초기화
            launcherConfig = LauncherConfig.Load();
            updateManager = new UpdateManager(httpClient, launcherConfig);

            _ = InitializeLauncherAsync();
        }

        private async Task InitializeLauncherAsync()
        {
            UpdateLauncherVersionDisplay();
            UpdateGameVersionDisplay();  // 여기 추가
            InitializeSensor();
            await CheckAllStatus();
        }

        /// <summary>
        /// 런처 버전을 좌측 상단에 표시
        /// </summary>
        private void UpdateLauncherVersionDisplay()
        {
            try
            {
                var localVersion = LocalVersionInfo.Load();
                LauncherVersionText.Text = $"Terra ParkGolf Launcher v{localVersion.LauncherVersion}";
                this.Title = $"Terra ParkGolf Launcher {localVersion.LauncherVersion}";
                Console.WriteLine($"런처 버전 표시: {localVersion.LauncherVersion}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"버전 표시 오류: {ex.Message}");
                LauncherVersionText.Text = "Terra ParkGolf Launcher v0.0.1";
            }
        }

        private void InitializeSensor()
        {
            try
            {
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XcamAdapt64.dll");
                if (!File.Exists(dllPath))
                {
                    Console.WriteLine($"[ERROR] XcamAdapt64.dll 없음: {dllPath}");
                    sensorHandle = IntPtr.Zero;
                    return;
                }

                sensorHandle = CR2_init(0, 1, 0, 0, 0, 0);

                if (sensorHandle == IntPtr.Zero)
                {
                    Console.WriteLine("[CR2_init 실패]");
                    return;
                }

                Console.WriteLine($"[CR2_init 성공] Handle = 0x{sensorHandle.ToInt64():X}");

                if (!SensorHelper.StartSensor(sensorHandle))
                {
                    Console.WriteLine("[경고] 센서 시작 실패");
                }
                else
                {
                    Console.WriteLine("[센서 시작 성공]");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[센서 초기화 예외] {ex.Message}");
                sensorHandle = IntPtr.Zero;
            }
        }

        private async Task CheckAllStatus()
        {
            try
            {
                // 진행 표시
                Dispatcher.Invoke(() =>
                {
                    ProgressPanel.Visibility = Visibility.Visible;
                });

                UpdateProgressBar(10, "센서 상태 확인 중...");
                await Task.Run(() => CheckSensorStatus());

                UpdateProgressBar(40, "시리얼 포트 확인 중...");
                await Task.Run(() => CheckSerialPortStatus());

                UpdateProgressBar(70, "업데이트 확인 중...");
                await CheckForUpdates();

                UpdateProgressBar(100, "상태 확인 완료");

                // 전체 상태 확인
                bool allOk = sensorConnected && serialConnected;
                UpdateSystemStatusIndicator(allOk);

                string statusMessage = allOk
                    ? "정상 동작하였습니다."
                    : "장치 연결을 확인해주세요.";

                UpdateStatusMessage(statusMessage);

                await Task.Delay(2000); // 2초 대기
            }
            catch (Exception ex)
            {
                Console.WriteLine($"상태 확인 오류: {ex.Message}");
                UpdateStatusMessage("상태 확인 중 오류가 발생했습니다.");
                UpdateSystemStatusIndicator(false);
            }
            finally
            {
                // 진행 표시 숨김
                Dispatcher.Invoke(() =>
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void CheckSensorStatus()
        {
            try
            {
                if (sensorHandle == IntPtr.Zero)
                {
                    sensorConnected = false;
                    Dispatcher.Invoke(() => UpdateSensorUI(Colors.Red, "미초기화"));
                    return;
                }

                bool ok = SensorHelper.CheckSensorStatus(sensorHandle, out uint status, out string txt);
                sensorConnected = ok && status != SensorHelper.CR2STATUS_DISCONNECT;

                Dispatcher.Invoke(() => UpdateSensorUI(
                    sensorConnected ? Colors.Green : Colors.Red,
                    sensorConnected ? txt : txt));
            }
            catch (Exception ex)
            {
                sensorConnected = false;
                Dispatcher.Invoke(() => UpdateSensorUI(Colors.Red, "오류"));
                Console.WriteLine($"센서 확인 오류: {ex.Message}");
            }
        }

        private void UpdateSensorUI(Color color, string text)
        {
            SensorStatusIndicator.Fill = new SolidColorBrush(color);
            SensorStatusText.Text = text;
        }

        private void CheckSerialPortStatus()
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames();
                serialConnected = false;

                bool timeoutOccurred = !Task.Run(() =>
                {
                    foreach (string portName in portNames)
                    {
                        try
                        {
                            using (SerialPort port = new SerialPort(portName))
                            {
                                port.BaudRate = 9600;
                                port.Open();
                                serialConnected = true;
                                break;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }).Wait(3000);

                if (timeoutOccurred)
                {
                    Console.WriteLine("시리얼 포트 체크 타임아웃 발생");
                }

                Dispatcher.Invoke(() =>
                {
                    if (serialConnected)
                    {
                        SerialStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                        SerialStatusText.Text = "연결됨";
                    }
                    else
                    {
                        SerialStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                        SerialStatusText.Text = timeoutOccurred ? "타임아웃" : "미연결";
                    }
                });
            }
            catch (Exception ex)
            {
                serialConnected = false;
                Dispatcher.Invoke(() =>
                {
                    SerialStatusIndicator.Fill = new SolidColorBrush(Colors.Red);
                    SerialStatusText.Text = "오류";
                });
                Console.WriteLine($"시리얼 포트 확인 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 서버에서 업데이트 확인 (개선된 버전)
        /// </summary>
        private async Task CheckForUpdates()
        {
            try
            {
                Console.WriteLine("=== 업데이트 체크 시작 ===");

                // UpdateManager를 통해 서버에서 업데이트 정보 리스트 가져오기
                currentUpdateInfoList = await updateManager.FetchUpdateInfoList();

                if (currentUpdateInfoList == null || currentUpdateInfoList.Versions == null)
                {
                    throw new Exception("업데이트 정보를 가져올 수 없습니다.");
                }

                // 대기 중인 업데이트 확인
                var pendingGameUpdates = currentUpdateInfoList.GetPendingGameUpdates();
                var pendingLauncherUpdates = currentUpdateInfoList.GetPendingLauncherUpdates();
                bool needsUpdate = pendingGameUpdates.Any() || pendingLauncherUpdates.Any();

                Console.WriteLine($"업데이트 체크 결과:");
                Console.WriteLine($"  대기 중인 게임 업데이트: {pendingGameUpdates.Count}개");
                Console.WriteLine($"  대기 중인 런처 업데이트: {pendingLauncherUpdates.Count}개");

                Dispatcher.Invoke(() =>
                {
                    if (needsUpdate)
                    {
                        UpdateStatusIndicator.Fill = new SolidColorBrush(Colors.Orange);
                        UpdateStatusText.Text = currentUpdateInfoList.GetUpdateSummary();
                        UpdateButton.Visibility = Visibility.Visible;
                        UpdateButton.IsEnabled = true;

                        // ⭐ 릴리즈 노트가 있으면 표시
                        string combinedNotes = currentUpdateInfoList.GetCombinedReleaseNotes();
                        if (!string.IsNullOrWhiteSpace(combinedNotes))
                        {
                            ReleaseNotesText.Text = combinedNotes;
                            ReleaseNotesBorder.Visibility = Visibility.Visible;
                            Console.WriteLine($"[UI] 릴리즈 노트 표시");
                        }
                        else
                        {
                            ReleaseNotesBorder.Visibility = Visibility.Collapsed;
                            Console.WriteLine("[UI] 릴리즈 노트 없음");
                        }
                    }
                    else
                    {
                        UpdateStatusIndicator.Fill = new SolidColorBrush(Colors.Green);
                        UpdateStatusText.Text = "최신";
                        UpdateButton.Visibility = Visibility.Collapsed;
                        ReleaseNotesBorder.Visibility = Visibility.Collapsed;
                    }
                });

                Console.WriteLine("=== 업데이트 체크 완료 ===");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"서버 연결 실패: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    UpdateStatusIndicator.Fill = new SolidColorBrush(Colors.Gray);
                    UpdateStatusText.Text = "서버 연결 실패";
                    UpdateButton.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"업데이트 확인 오류: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    UpdateStatusIndicator.Fill = new SolidColorBrush(Colors.Orange);
                    UpdateStatusText.Text = "확인 실패";
                    UpdateButton.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void UpdateProgressBar(double value, string text)
        {
            Dispatcher.Invoke(() =>
            {
                if (ProgressPanel != null)
                {
                    ProgressPanel.Visibility = Visibility.Visible;
                }
                if (ProgressBar != null)
                {
                    ProgressBar.Value = value;
                }
                if (ProgressText != null)
                {
                    ProgressText.Text = text;
                }
            });
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 업데이트 중이면 취소
            if (updateCancellationTokenSource != null && !updateCancellationTokenSource.IsCancellationRequested)
            {
                var result = MessageBox.Show(
                    "업데이트가 진행 중입니다. 종료하시겠습니까?",
                    "확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    updateCancellationTokenSource.Cancel();
                }
                else
                {
                    return;
                }
            }

            this.Close();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshButton.IsEnabled = false;
            UpdateLauncherVersionDisplay();
            await CheckAllStatus();
            RefreshButton.IsEnabled = true;
        }

        /// <summary>
        /// 업데이트 버튼 클릭 (Show 기반으로 수정)
        /// </summary>
        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentUpdateInfoList == null)
            {
                MessageBox.Show("업데이트 정보를 먼저 확인해주세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 대기 중인 업데이트 확인
            var pendingGameUpdates = currentUpdateInfoList.GetPendingGameUpdates();
            var pendingLauncherUpdates = currentUpdateInfoList.GetPendingLauncherUpdates();

            if (!pendingGameUpdates.Any() && !pendingLauncherUpdates.Any())
            {
                UpdatePopupWindow.ShowAlreadyLatestVersion(this);
                return;
            }

            // 업데이트 팝업 생성
            var latestVersion = pendingGameUpdates.Any()
                ? pendingGameUpdates.Last().UpdateIndex
                : pendingLauncherUpdates.Last().LauncherIndex;

            var updatePopup = new UpdatePopupWindow(latestVersion)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // ⭐ Show()로 변경 (모달이 아닌 일반 창)
            updatePopup.Show();

            // 사용자가 "네"를 클릭할 때까지 대기
            while (!updatePopup.UpdateConfirmed && !updatePopup.UpdateCancelled && updatePopup.IsLoaded)
            {
                await Task.Delay(100);
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            }

            // 취소한 경우
            if (updatePopup.UpdateCancelled || !updatePopup.UpdateConfirmed)
            {
                Console.WriteLine("사용자가 업데이트를 취소했습니다.");
                updatePopup.Close();
                return;
            }

            // '네' 클릭 시 - 업데이트 진행
            Console.WriteLine("업데이트를 시작합니다.");

            // CancellationToken 생성
            updateCancellationTokenSource = new CancellationTokenSource();

            try
            {
                // 게임 업데이트
                if (pendingGameUpdates.Any())
                {
                    Console.WriteLine($"=== {pendingGameUpdates.Count}개의 게임 업데이트 시작 ===");
                    await updateManager.PerformAllPendingGameUpdates(
                        currentUpdateInfoList,
                        (progress, statusMsg) =>
                        {
                            // ⭐ 팝업이 열려있는 상태에서 진행바 업데이트
                            updatePopup.UpdateProgress(progress, statusMsg);
                        },
                        updateCancellationTokenSource.Token);

                    Console.WriteLine("=== 모든 게임 업데이트 완료 ===");
                }

                // 런처 업데이트
                if (pendingLauncherUpdates.Any())
                {
                    var latestLauncher = pendingLauncherUpdates.Last();
                    Console.WriteLine($"=== 런처 업데이트 시작 (v{latestLauncher.LauncherIndex}) ===");
                    await updateManager.PerformLauncherUpdate(
                        latestLauncher.LauncherUrl,
                        latestLauncher.LauncherHash,
                        latestLauncher.LauncherIndex,  // ⭐ 새 런처 버전 전달
                        (progress, statusMsg) =>
                        {
                            updatePopup.UpdateProgress(progress, statusMsg);
                        },
                        updateCancellationTokenSource.Token);

                    Console.WriteLine("=== 런처 업데이트 완료 - 애플리케이션 종료 중 ===");

                    await Task.Delay(500);
                    Environment.Exit(0);
                    return;
                }

                // ⭐⭐⭐ 게임 업데이트만 있었을 때 완료 상태 표시
                Console.WriteLine("[게임 업데이트만 완료] 팝업 완료 상태 표시 시작");
                if (updatePopup.IsLoaded)
                {
                    Console.WriteLine("[게임 업데이트만 완료] updatePopup.UpdateCompleted(true) 호출");
                    updatePopup.UpdateCompleted(true);  // ⭐ 추가: 완료 상태 표시
                    Console.WriteLine("[게임 업데이트만 완료] updatePopup.UpdateCompleted(true) 호출 완료");
                }
                else
                {
                    Console.WriteLine("[게임 업데이트만 완료] ⚠️ 팝업이 로드되지 않았습니다!");
                }

                // ⭐ 버전 정보 재로드 (파일에서 새로 읽기)
                var reloadedVersion = LocalVersionInfo.Load();
                Console.WriteLine($"[버전 재로드] 런처: {reloadedVersion.LauncherVersion}, 게임: {reloadedVersion.GameVersion}");

                // 버전 표시 갱신
                UpdateLauncherVersionDisplay();
                UpdateGameVersionDisplay();

                // ⭐ currentUpdateInfoList의 로컬 버전 정보 동기화 (메모리)
                currentUpdateInfoList.CurrentGameVersion = reloadedVersion.GameVersion;
                currentUpdateInfoList.CurrentLauncherVersion = reloadedVersion.LauncherVersion;
                Console.WriteLine($"[메모리 동기화] CurrentGameVersion: {currentUpdateInfoList.CurrentGameVersion}");

            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[업데이트 취소] 팝업 실패 상태 표시");
                if (updatePopup.IsLoaded)
                {
                    updatePopup.UpdateCompleted(false);
                }
                MessageBox.Show("업데이트가 취소되었습니다.", "취소",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[업데이트 오류] 팝업 실패 상태 표시");
                if (updatePopup.IsLoaded)
                {
                    updatePopup.UpdateCompleted(false);
                }
                MessageBox.Show($"업데이트 실패:\n{ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"업데이트 오류 상세: {ex}");
            }
            finally
            {
                updateCancellationTokenSource?.Dispose();
                updateCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 게임 업데이트 수행
        /// </summary>
        //private async Task PerformGameUpdate()
        //{
        //    try
        //    {
        //        Console.WriteLine("=== 게임 업데이트 시작 ===");

        //        await updateManager.PerformGameUpdate(
        //            currentUpdateInfo.UpdateFileUrl,
        //            currentUpdateInfo.UpdateFileHash,
        //            (progress, message) =>
        //            {
        //                UpdateProgressBar(progress, message);
        //            },
        //            updateCancellationTokenSource.Token);

        //        Console.WriteLine("=== 게임 업데이트 완료 ===");

        //        // ⭐⭐⭐ 여기부터 추가 ⭐⭐⭐
        //        // 게임 버전 정보 갱신
        //        try
        //        {
        //            Console.WriteLine("[게임 업데이트] 버전 정보 갱신 시작");
        //            var localVersion = LocalVersionInfo.Load();
        //            string oldVersion = localVersion.GameVersion;
        //            localVersion.UpdateGameVersion(currentUpdateInfo.UpdateIndex);
        //            Console.WriteLine($"[게임 업데이트] 버전 갱신 완료: {oldVersion} → {currentUpdateInfo.UpdateIndex}");

        //            // 버전 갱신 확인
        //            var verifiedVersion = LocalVersionInfo.Load();
        //            Console.WriteLine($"[버전 확인] 저장된 게임 버전: {verifiedVersion.GameVersion}");
        //            Console.WriteLine($"[버전 확인] 갱신 시각: {verifiedVersion.LastUpdateDate:yyyy-MM-dd HH:mm:ss}");
        //        }
        //        catch (Exception versionEx)
        //        {
        //            Console.WriteLine($"[경고] 버전 정보 갱신 실패: {versionEx.Message}");
        //            Console.WriteLine($"[경고] 스택트레이스: {versionEx.StackTrace}");
        //            // 업데이트 자체는 성공했으므로 예외를 던지지 않음
        //        }
        //        // ⭐⭐⭐ 여기까지 추가 ⭐⭐⭐

        //        // 게임 업데이트 완료 후 버전 표시 갱신
        //        UpdateGameVersionDisplay();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"게임 업데이트 실패: {ex.Message}");
        //        throw;
        //    }
        //}

        /// <summary>
        /// 런처 업데이트 수행
        /// </summary>
        //private async Task PerformLauncherUpdate()
        //{
        //    try
        //    {
        //        Console.WriteLine("=== 런처 업데이트 시작 ===");

        //        bool success = await updateManager.PerformLauncherUpdate(
        //            currentUpdateInfo.LauncherUrl,
        //            currentUpdateInfo.LauncherHash,
        //            (progress, message) =>
        //            {
        //                UpdateProgressBar(progress, message);
        //            },
        //            updateCancellationTokenSource.Token);

        //        if (success)
        //        {
        //            Console.WriteLine("=== 런처 업데이트 완료 - 재시작 중 ===");
        //            // 런처가 재시작되면서 이 프로세스는 종료됨
        //            Application.Current.Shutdown();
        //        }
        //        else
        //        {
        //            throw new Exception("런처 업데이트에 실패했습니다.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"런처 업데이트 실패: {ex.Message}");
        //        throw;
        //    }
        //}

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string gameExePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    launcherConfig.GameExecutablePath);

                if (!File.Exists(gameExePath))
                {
                    MessageBox.Show(
                        $"게임 실행 파일을 찾을 수 없습니다.\n경로: {gameExePath}",
                        "오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // 게임 실행
                Process.Start(gameExePath);
                Console.WriteLine($"게임 실행: {gameExePath}");

                // 런처 종료
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"게임 실행 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"게임 실행 오류: {ex}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 업데이트 중이면 취소
            updateCancellationTokenSource?.Cancel();
            updateCancellationTokenSource?.Dispose();

            httpClient?.Dispose();

            if (sensorHandle != IntPtr.Zero)
            {
                try
                {
                    SensorHelper.StopSensor(sensorHandle);
                    SensorHelper.TurnOffLED(sensorHandle);
                    CR2_delete(sensorHandle);
                    sensorHandle = IntPtr.Zero;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[센서 정리 오류] {ex.Message}");
                }
            }

            base.OnClosed(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }


        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 비밀번호 인증 대화상자 표시
                var passwordDialog = new PasswordDialog
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                bool? passwordResult = passwordDialog.ShowDialog();

                // 2. 비밀번호 인증 실패 시 중단
                if (passwordResult != true)
                {
                    Console.WriteLine("비밀번호 인증 실패 또는 취소됨");
                    return;
                }

                Console.WriteLine("비밀번호 인증 성공 - 옵션 창 열기");

                // 3. 인증 성공 시 옵션 창 표시
                var optionsWindow = new OptionsWindow
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                bool? optionsResult = optionsWindow.ShowDialog();

                if (optionsResult == true)
                {
                    Console.WriteLine("게임 옵션이 저장되었습니다.");
                    // 필요시 추가 작업 수행
                }
                else
                {
                    Console.WriteLine("게임 옵션 설정이 취소되었습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"옵션 창 열기 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"옵션 창 오류: {ex}");
            }
        }

        private void RestartWindows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 재부팅 명령 실행 (shutdown /r /t 0)
                Process.Start("shutdown", "/r /t 0");

                // 명령을 실행한 후 애플리케이션 자체를 종료합니다.
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Windows 재부팅 명령어 실행 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Windows 재부팅 오류: {ex}");
            }
        }

        private void ShutdownWindows_Click(object sender, RoutedEventArgs e)
        {
            // 1. 사용자에게 종료 여부 확인
            MessageBoxResult result = MessageBox.Show(
                "정말로 컴퓨터를 종료하시겠습니까? 저장되지 않은 데이터는 손실될 수 있습니다.",
                "시스템 종료 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                // 사용자가 취소했을 경우
                Console.WriteLine("컴퓨터 종료가 취소되었습니다.");
                return;
            }

            try
            {
                // 2. 종료 명령 실행 (shutdown /s /t 0)
                // /s: 종료(Shut Down), /t 0: 지연 시간 0초 (즉시)
                Process.Start("shutdown", "/s /t 0");

                // 명령을 실행한 후 애플리케이션 자체를 종료합니다.
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Windows 종료 명령어 실행 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"Windows 종료 오류: {ex}");
            }
        }


        private void LaunchTeamViewer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("[TeamViewer] 실행 시도...");

                // 1. Shell Execute로 먼저 시도 (PATH에 있는 경우)
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "teamviewer",
                        UseShellExecute = true
                    };

                    Process.Start(psi);
                    Console.WriteLine("[TeamViewer] Shell Execute로 실행 성공");
                    return;
                }
                catch
                {
                    Console.WriteLine("[TeamViewer] Shell Execute 실패, 경로 검색 시작...");
                }

                // 2. 레지스트리에서 검색
                string teamViewerPath = FindTeamViewerFromRegistry();

                // 3. 일반 경로에서 검색
                if (teamViewerPath == null)
                {
                    teamViewerPath = FindTeamViewerPath();
                }

                // 4. 실행
                if (teamViewerPath != null)
                {
                    Console.WriteLine($"[TeamViewer] 실행 경로: {teamViewerPath}");

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = teamViewerPath,
                        UseShellExecute = true
                    };

                    Process.Start(psi);
                    Console.WriteLine("[TeamViewer] 실행 성공");
                }
                else
                {
                    // 설치 안내
                    var result = MessageBox.Show(
                        "TeamViewer 실행 파일을 찾을 수 없습니다.\n\n" +
                        "TeamViewer를 설치하시겠습니까?",
                        "TeamViewer를 찾을 수 없음",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // TeamViewer 다운로드 페이지 열기
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://www.teamviewer.com/ko/download/",
                            UseShellExecute = true
                        });
                    }

                    Console.WriteLine("[TeamViewer] 실행 파일을 찾을 수 없음");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"TeamViewer 실행 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"[TeamViewer] 실행 오류: {ex}");
            }
        }

        /// <summary>
        /// 레지스트리에서 TeamViewer 경로 찾기
        /// </summary>
        private string FindTeamViewerFromRegistry()
        {
            try
            {
                Console.WriteLine("[TeamViewer] 레지스트리 검색 중...");

                // 확인할 레지스트리 키 목록
                string[] registryKeys = new string[]
                {
            @"SOFTWARE\TeamViewer",
            @"SOFTWARE\WOW6432Node\TeamViewer"
                };

                foreach (string keyPath in registryKeys)
                {
                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                        {
                            if (key != null)
                            {
                                // InstallLocation 값 확인
                                object installLocation = key.GetValue("InstallLocation");
                                if (installLocation != null)
                                {
                                    string path = Path.Combine(installLocation.ToString(), "TeamViewer.exe");
                                    if (File.Exists(path))
                                    {
                                        Console.WriteLine($"[TeamViewer] 레지스트리에서 찾음: {path}");
                                        return path;
                                    }
                                }

                                // InstallPath 값 확인
                                object installPath = key.GetValue("InstallPath");
                                if (installPath != null)
                                {
                                    string path = Path.Combine(installPath.ToString(), "TeamViewer.exe");
                                    if (File.Exists(path))
                                    {
                                        Console.WriteLine($"[TeamViewer] 레지스트리에서 찾음: {path}");
                                        return path;
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // 이 키를 열 수 없으면 다음 키 시도
                        continue;
                    }
                }

                Console.WriteLine("[TeamViewer] 레지스트리에서 찾을 수 없음");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamViewer] 레지스트리 검색 오류: {ex.Message}");
            }

            return null;
        }


        /// <summary>
        /// 일반적인 경로에서 TeamViewer 찾기
        /// </summary>
        private string FindTeamViewerPath()
        {
            Console.WriteLine("[TeamViewer] 일반 경로 검색 중...");

            // 확인할 경로 목록
            List<string> possiblePaths = new List<string>();

            // 1. Program Files (x86) - 가장 일반적
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            possiblePaths.Add(Path.Combine(programFilesX86, "TeamViewer", "TeamViewer.exe"));

            // 2. Program Files - 64비트 버전
            string programFiles = Environment.GetEnvironmentVariable("ProgramW6432");
            if (string.IsNullOrEmpty(programFiles))
            {
                programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            }
            possiblePaths.Add(Path.Combine(programFiles, "TeamViewer", "TeamViewer.exe"));

            // 3. AppData\Local - 사용자별 설치
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            possiblePaths.Add(Path.Combine(localAppData, "TeamViewer", "TeamViewer.exe"));

            // 4. AppData\Roaming
            string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            possiblePaths.Add(Path.Combine(roamingAppData, "TeamViewer", "TeamViewer.exe"));

            // 5. Downloads 폴더
            string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            possiblePaths.Add(Path.Combine(downloads, "TeamViewer.exe"));

            // 6. 데스크톱
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            possiblePaths.Add(Path.Combine(desktop, "TeamViewer.exe"));

            // 각 경로 확인
            foreach (string path in possiblePaths)
            {
                Console.WriteLine($"[TeamViewer] 확인: {path}");
                if (File.Exists(path))
                {
                    Console.WriteLine($"[TeamViewer] 찾음: {path}");
                    return path;
                }
            }

            Console.WriteLine("[TeamViewer] 일반 경로에서 찾을 수 없음");
            return null;
        }


        private void UpdateGameVersionDisplay()
        {
            try
            {
                // local_version.json에서 직접 로드 (가장 신뢰할 수 있음)
                var localVersion = LocalVersionInfo.Load();
                string gameVersion = localVersion.GameVersion;

                if (string.IsNullOrWhiteSpace(gameVersion))
                {
                    gameVersion = "0.0.1";
                }

                Dispatcher.Invoke(() =>
                {
                    GameVersionText.Text = $"Game v{gameVersion}";
                });

                Console.WriteLine($"게임 버전: {gameVersion}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"게임 버전 표시 실패: {ex.Message}");
                Dispatcher.Invoke(() =>
                {
                    GameVersionText.Text = "Game v오류";
                });
            }
        }

        /// <summary>
        /// 버튼 영역에 마우스가 올라갔을 때
        /// </summary>
        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                // 밝기 효과
                border.Opacity = 1.2;

                // 약간 확대 효과
                var scaleTransform = new ScaleTransform(1.05, 1.05);
                border.RenderTransform = scaleTransform;
                border.RenderTransformOrigin = new Point(0.5, 0.5);

                // 애니메이션 효과
                var opacityAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(150)
                };

                var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 1.05,
                    Duration = TimeSpan.FromMilliseconds(150)
                };

                var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 1.05,
                    Duration = TimeSpan.FromMilliseconds(150)
                };

                border.BeginAnimation(Border.OpacityProperty, opacityAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
            }
        }

        /// <summary>
        /// 버튼 영역에서 마우스가 벗어났을 때
        /// </summary>
        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                // 애니메이션으로 원래대로
                var opacityAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = border.Opacity,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(150)
                };

                if (border.RenderTransform is ScaleTransform scaleTransform)
                {
                    var scaleXAnimation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = scaleTransform.ScaleX,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };

                    var scaleYAnimation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = scaleTransform.ScaleY,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };

                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
                }

                border.BeginAnimation(Border.OpacityProperty, opacityAnimation);
            }
        }

        /// <summary>
        /// 시스템 점검 버튼 클릭 (새로 추가)
        /// </summary>
        private async void SystemCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("[시스템 점검] 시작");

                // 진행 표시
                Dispatcher.Invoke(() =>
                {
                    ProgressPanel.Visibility = Visibility.Visible;
                    fr_launcher_state.Text = "시스템 점검 중...";
                });

                // 상태 재확인
                await CheckAllStatus();

                Console.WriteLine("[시스템 점검] 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"시스템 점검 실패: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"[시스템 점검] 오류: {ex}");
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void UpdateStatusMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                fr_launcher_state.Text = message;
            });
        }

        /// <summary>
        /// 시스템 상태 인디케이터 업데이트 (수정)
        /// </summary>
        private void UpdateSystemStatusIndicator(bool allOk)
        {
            Dispatcher.Invoke(() =>
            {
                icon_circle_state_green.Fill = new SolidColorBrush(
                    allOk ? Colors.Green : Colors.Red);
            });
        }
    }

}