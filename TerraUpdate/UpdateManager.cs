using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
    /// <summary>
    /// 업데이트 관리 클래스
    /// </summary>
    public class UpdateManager
    {
        private readonly HttpClient httpClient;
        private readonly LauncherConfig launcherConfig;
        private readonly string tempDownloadPath;
        private readonly string backupPath;

        public UpdateManager(HttpClient client, LauncherConfig config)
        {
            httpClient = client;
            launcherConfig = config;
            tempDownloadPath = Path.Combine(Path.GetTempPath(), "TerraUpdate");
            backupPath = Path.Combine(Path.GetTempPath(), "TerraBackup");

            // 임시 디렉토리 생성
            Directory.CreateDirectory(tempDownloadPath);
            Directory.CreateDirectory(backupPath);
        }

        /// <summary>
        /// 서버에서 업데이트 정보 리스트 가져오기 (채널 적용)
        /// </summary>
        public async Task<GameUpdateInfoList> FetchUpdateInfoList()
        {
            try
            {
                // 채널에 맞는 API URL 가져오기
                string apiUrl = launcherConfig.GetVersionApiUrl();
                string fullUrl = launcherConfig.GetFullUrl(apiUrl);

                Console.WriteLine($"[UpdateManager] 업데이트 정보 요청: {fullUrl}");
                Console.WriteLine($"[UpdateManager] 설정된 채널: {launcherConfig.Channel}");

                string response = await httpClient.GetStringAsync(fullUrl);
                Console.WriteLine($"[UpdateManager] 서버 응답 받음 (길이: {response.Length})");

                // JSON 배열 파싱
                var versions = JsonConvert.DeserializeObject<List<GameUpdateInfo>>(response);
                Console.WriteLine($"[UpdateManager] 파싱된 버전 수: {versions?.Count ?? 0}");

                if (versions != null)
                {
                    foreach (var v in versions)
                    {
                        Console.WriteLine($"  - 런처 v{v.LauncherIndex}, 게임 v{v.UpdateIndex}, 채널: {v.Shipping}");
                    }
                }

                // 로컬 버전 로드
                var localVersion = LocalVersionInfo.Load();
                Console.WriteLine($"[UpdateManager] 로컬 버전 - 런처: {localVersion.LauncherVersion}, 게임: {localVersion.GameVersion}");

                // GameUpdateInfoList 생성
                var updateInfoList = new GameUpdateInfoList
                {
                    Versions = versions ?? new List<GameUpdateInfo>(),
                    CurrentLauncherVersion = localVersion.LauncherVersion,
                    CurrentGameVersion = localVersion.GameVersion,
                    CurrentChannel = launcherConfig.Channel  // ⭐ 채널 설정
                };

                Console.WriteLine($"[UpdateManager] 업데이트 정보 리스트 생성 완료 (채널: {updateInfoList.CurrentChannel})");

                return updateInfoList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateManager] 업데이트 정보 가져오기 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 모든 대기 중인 게임 업데이트 순차 수행
        /// </summary>
        public async Task PerformAllPendingGameUpdates(
            GameUpdateInfoList updateInfoList,
            Action<int, string> progressCallback,
            CancellationToken cancellationToken)
        {
            var pendingUpdates = updateInfoList.GetPendingGameUpdates();

            if (!pendingUpdates.Any())
            {
                Console.WriteLine("[게임 업데이트] 대기 중인 업데이트가 없습니다.");
                return;
            }

            Console.WriteLine($"[게임 업데이트] {pendingUpdates.Count}개의 업데이트 순차 처리 시작");

            int totalUpdates = pendingUpdates.Count;
            for (int i = 0; i < totalUpdates; i++)
            {
                var update = pendingUpdates[i];
                Console.WriteLine($"[게임 업데이트] {i + 1}/{totalUpdates}: v{update.UpdateIndex} ({update.Shipping}) 처리 중...");

                // 각 업데이트의 진행률 계산 (전체 진행률에 반영)
                int baseProgress = (i * 100) / totalUpdates;
                int progressRange = 100 / totalUpdates;

                await PerformSingleGameUpdate(
                    update.UpdateFileUrl,
                    update.UpdateFileHash,
                    update.UpdateIndex,
                    (progress, message) =>
                    {
                        int totalProgress = baseProgress + (progress * progressRange / 100);
                        progressCallback?.Invoke(totalProgress, $"[{i + 1}/{totalUpdates}] {message}");
                    },
                    cancellationToken);

                Console.WriteLine($"[게임 업데이트] {i + 1}/{totalUpdates}: v{update.UpdateIndex} 완료");
            }

            Console.WriteLine($"[게임 업데이트] 모든 업데이트 완료");
        }

        /// <summary>
        /// 단일 게임 업데이트 수행
        /// </summary>
        private async Task PerformSingleGameUpdate(
            string downloadUrl,
            string expectedHash,
            string newVersion,
            Action<int, string> progressCallback,
            CancellationToken cancellationToken)
        {
            string zipPath = Path.Combine(tempDownloadPath, $"game_update_{newVersion}.zip");
            string extractPath = Path.Combine(tempDownloadPath, $"game_extract_{newVersion}");

            try
            {
                // 1. 다운로드
                progressCallback?.Invoke(10, $"v{newVersion} 다운로드 중...");
                await DownloadFile(downloadUrl, zipPath, cancellationToken);
                Console.WriteLine($"[게임 업데이트] 다운로드 완료: {zipPath}");

                // 2. 해시 검증
                progressCallback?.Invoke(40, $"v{newVersion} 무결성 검증 중...");
                if (!await VerifyFileHash(zipPath, expectedHash))
                {
                    throw new Exception($"v{newVersion} 파일 무결성 검증 실패");
                }
                Console.WriteLine($"[게임 업데이트] 해시 검증 통과");

                // 3. 압축 해제
                progressCallback?.Invoke(60, $"v{newVersion} 압축 해제 중...");
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                Console.WriteLine($"[게임 업데이트] 압축 해제 완료: {extractPath}");

                // 4. 파일 복사
                progressCallback?.Invoke(80, $"v{newVersion} 파일 업데이트 중...");
                string gameRootPath = AppDomain.CurrentDomain.BaseDirectory;
                CopyDirectory(extractPath, gameRootPath, true);
                Console.WriteLine($"[게임 업데이트] 파일 복사 완료");

                // 5. 버전 정보 업데이트
                var localVersion = LocalVersionInfo.Load();
                localVersion.UpdateGameVersion(newVersion);
                Console.WriteLine($"[게임 업데이트] 버전 정보 업데이트: {newVersion}");

                progressCallback?.Invoke(100, $"v{newVersion} 업데이트 완료");
            }
            finally
            {
                // 임시 파일 정리
                try
                {
                    if (File.Exists(zipPath))
                        File.Delete(zipPath);
                    if (Directory.Exists(extractPath))
                        Directory.Delete(extractPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[게임 업데이트] 임시 파일 정리 실패: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 런처 업데이트 수행
        /// ⭐ Sharing violation 해결 개선
        /// </summary>
        public async Task<bool> PerformLauncherUpdate(
            string downloadUrl,
            string expectedHash,
            string newLauncherVersion,
            Action<int, string> progressCallback,
            CancellationToken cancellationToken)
        {
            string zipPath = Path.Combine(tempDownloadPath, "launcher_update.zip");
            string extractPath = Path.Combine(tempDownloadPath, "launcher_extract");
            string batchPath = Path.Combine(tempDownloadPath, "update_launcher.bat");

            try
            {
                // 1. 다운로드
                progressCallback?.Invoke(20, "런처 다운로드 중...");
                await DownloadFile(downloadUrl, zipPath, cancellationToken);
                Console.WriteLine($"[런처 업데이트] 다운로드 완료: {zipPath}");

                // 2. 해시 검증
                progressCallback?.Invoke(40, "무결성 검증 중...");
                if (!await VerifyFileHash(zipPath, expectedHash))
                {
                    throw new Exception("런처 파일 무결성 검증 실패");
                }

                // 3. 압축 해제
                progressCallback?.Invoke(60, "압축 해제 중...");
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // ⭐ 4. 런처 버전 미리 업데이트 (배치 실행 전!)
                progressCallback?.Invoke(70, "버전 정보 업데이트 중...");
                Console.WriteLine($"[런처 업데이트] 버전 저장 시작");
                try
                {
                    var localVersion = LocalVersionInfo.Load();
                    Console.WriteLine($"[런처 업데이트] 현재 버전 로드됨 - 런처: {localVersion.LauncherVersion}");

                    localVersion.UpdateLauncherVersion(newLauncherVersion);
                    Console.WriteLine($"[런처 업데이트] 버전 정보 저장 완료: {newLauncherVersion}");

                    // 저장 확인
                    var verifyVersion = LocalVersionInfo.Load();
                    Console.WriteLine($"[런처 업데이트] 저장 확인 - 런처: {verifyVersion.LauncherVersion}");
                }
                catch (Exception versionEx)
                {
                    Console.WriteLine($"[런처 업데이트] 버전 저장 실패: {versionEx.Message}");
                    Console.WriteLine($"[런처 업데이트] 스택트레이스: {versionEx.StackTrace}");
                    throw;
                }

                // 5. 업데이트 배치 파일 생성
                progressCallback?.Invoke(80, "업데이트 준비 중...");
                string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
                string currentExeDir = Path.GetDirectoryName(currentExePath);

                CreateLauncherUpdateBatch(batchPath, extractPath, currentExeDir, currentExePath);

                // 6. 배치 파일 실행 및 종료
                progressCallback?.Invoke(90, "런처 재시작 중...");
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });

                await Task.Delay(500);
                Environment.Exit(0);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[런처 업데이트] 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 런처 업데이트 배치 파일 생성
        /// ⭐ PowerShell 스크립트를 별도 파일로 분리
        /// </summary>
        private void CreateLauncherUpdateBatch(string batchPath, string sourcePath, string targetPath, string exePath)
        {
            string exeName = Path.GetFileName(exePath);
            string psScriptPath = Path.Combine(Path.GetDirectoryName(batchPath), "update_copy.ps1");
            string logPath = Path.Combine(Path.GetDirectoryName(batchPath), "update.log");

            // 1️⃣ PowerShell 스크립트 생성
            var psScript = new StringBuilder();
            psScript.AppendLine("# PowerShell 파일 복사 스크립트");
            psScript.AppendLine($"$source = '{sourcePath}'");
            psScript.AppendLine($"$target = '{targetPath}'");
            psScript.AppendLine("");
            psScript.AppendLine("Write-Host '[PowerShell] 파일 복사 시작'");
            psScript.AppendLine("if (Test-Path $source) {");
            psScript.AppendLine("    Write-Host \"[PowerShell] 소스 확인: $source\"");
            psScript.AppendLine("    ");
            psScript.AppendLine("    # 타겟 디렉토리 생성");
            psScript.AppendLine("    if (-not (Test-Path $target)) {");
            psScript.AppendLine("        New-Item -ItemType Directory -Path $target -Force | Out-Null");
            psScript.AppendLine("        Write-Host \"[PowerShell] 타겟 디렉토리 생성됨: $target\"");
            psScript.AppendLine("    }");
            psScript.AppendLine("    ");
            psScript.AppendLine("    # 재귀적으로 파일 복사");
            psScript.AppendLine("    Copy-Item -Path \"$source\\*\" -Destination $target -Recurse -Force -ErrorAction SilentlyContinue");
            psScript.AppendLine("    Write-Host '[PowerShell] 파일 복사 완료'");
            psScript.AppendLine("    ");
            psScript.AppendLine("    # 복사 결과 확인");
            psScript.AppendLine("    $files = Get-ChildItem -Path $target -Recurse");
            psScript.AppendLine("    Write-Host \"[PowerShell] 복사된 파일 수: $($files.Count)\"");
            psScript.AppendLine("} else {");
            psScript.AppendLine("    Write-Host \"[PowerShell] 오류: 소스 디렉토리 없음 - $source\"");
            psScript.AppendLine("}");

            File.WriteAllText(psScriptPath, psScript.ToString(), Encoding.UTF8);
            Console.WriteLine($"[PowerShell 스크립트] 생성: {psScriptPath}");

            // 2️⃣ 배치 파일 생성
            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal enabledelayedexpansion");
            sb.AppendLine("chcp 65001 > nul");
            sb.AppendLine("");
            sb.AppendLine($"set \"EXE_NAME={exeName}\"");
            sb.AppendLine($"set \"TARGET_DIR={targetPath}\"");
            sb.AppendLine($"set \"SOURCE_DIR={sourcePath}\"");
            sb.AppendLine($"set \"PS_SCRIPT={psScriptPath}\"");
            sb.AppendLine($"set \"LOG_FILE={logPath}\"");
            sb.AppendLine("");
            sb.AppendLine("echo ============================================");
            sb.AppendLine("echo [시작] 런처 업데이트 배치 시작");
            sb.AppendLine("echo 시간: %date% %time%");
            sb.AppendLine("echo ============================================");
            sb.AppendLine("timeout /t 1 /nobreak > nul");
            sb.AppendLine("");

            // 3️⃣ 프로세스 종료 대기
            sb.AppendLine("echo [1단계] TerraUpdate.exe 종료 대기 중...");
            sb.AppendLine("set \"procCount=0\"");
            sb.AppendLine(":wait_process");
            sb.AppendLine("tasklist /FI \"IMAGENAME eq TerraUpdate.exe\" 2>nul | find /I \"TerraUpdate.exe\" > nul");
            sb.AppendLine("if \"!errorlevel!\"==\"0\" (");
            sb.AppendLine("  if !procCount! lss 10 (");
            sb.AppendLine("    set /a procCount+=1");
            sb.AppendLine("    echo   - 대기 중... (!procCount!/10)");
            sb.AppendLine("    timeout /t 1 /nobreak > nul");
            sb.AppendLine("    goto wait_process");
            sb.AppendLine("  )");
            sb.AppendLine(")");
            sb.AppendLine("echo [1단계] 완료");
            sb.AppendLine("");

            // 4️⃣ 프로세스 강제 종료
            sb.AppendLine("echo [2단계] TerraUpdate.exe 강제 종료 중...");
            sb.AppendLine("taskkill /F /IM TerraUpdate.exe 2>nul");
            sb.AppendLine("timeout /t 5 /nobreak > nul");
            sb.AppendLine("echo [2단계] 완료");
            sb.AppendLine("");

            // 5️⃣ 파일 잠금 해제 대기
            sb.AppendLine("echo [3단계] 파일 잠금 해제 대기...");
            sb.AppendLine("timeout /t 3 /nobreak > nul");
            sb.AppendLine("echo [3단계] 완료");
            sb.AppendLine("");

            // 6️⃣ 소스 확인
            sb.AppendLine("echo [4단계] 소스 디렉토리 확인...");
            sb.AppendLine("if exist \"%SOURCE_DIR%\" (");
            sb.AppendLine("  echo   소스 디렉토리 존재함: %SOURCE_DIR%");
            sb.AppendLine("  echo   소스 파일 목록:");
            sb.AppendLine("  dir \"%SOURCE_DIR%\" /B");
            sb.AppendLine(") else (");
            sb.AppendLine("  echo   ERROR: 소스 디렉토리 없음 - %SOURCE_DIR%");
            sb.AppendLine("  pause");
            sb.AppendLine("  exit /b 1");
            sb.AppendLine(")");
            sb.AppendLine("echo [4단계] 완료");
            sb.AppendLine("");

            // 7️⃣ PowerShell로 파일 복사
            sb.AppendLine("echo [5단계] PowerShell로 파일 복사 중...");
            sb.AppendLine("echo   PS 스크립트: %PS_SCRIPT%");
            sb.AppendLine("powershell -NoProfile -ExecutionPolicy Bypass -File \"%PS_SCRIPT%\" >> \"%LOG_FILE%\" 2>&1");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("  echo   WARNING: PowerShell 스크립트 반환값 확인");
            sb.AppendLine(")");
            sb.AppendLine("timeout /t 5 /nobreak > nul");
            sb.AppendLine("echo [5단계] 완료");
            sb.AppendLine("");

            // 8️⃣ 복사 결과 확인
            sb.AppendLine("echo [6단계] 복사 결과 확인...");
            sb.AppendLine("if exist \"%TARGET_DIR%\" (");
            sb.AppendLine("  echo   타겟 디렉토리 존재 확인됨");
            sb.AppendLine("  echo   타겟 파일 목록:");
            sb.AppendLine("  dir \"%TARGET_DIR%\" /B");
            sb.AppendLine(") else (");
            sb.AppendLine("  echo   ERROR: 타겟 디렉토리 생성 실패");
            sb.AppendLine("  pause");
            sb.AppendLine("  exit /b 1");
            sb.AppendLine(")");
            sb.AppendLine("echo [6단계] 완료");
            sb.AppendLine("");

            // 9️⃣ 런처 실행
            sb.AppendLine("echo [7단계] 런처 시작 중...");
            sb.AppendLine("set \"NEW_EXE=%TARGET_DIR%\\%EXE_NAME%\"");
            sb.AppendLine("if exist \"%NEW_EXE%\" (");
            sb.AppendLine("  echo   ✓ 런처 파일 발견됨: %NEW_EXE%");
            sb.AppendLine("  timeout /t 1 /nobreak > nul");
            sb.AppendLine("  start \"\" \"%NEW_EXE%\"");
            sb.AppendLine("  echo   ✓ 런처 시작 완료");
            sb.AppendLine(") else (");
            sb.AppendLine("  echo   ✗ ERROR: 런처 파일 없음 - %NEW_EXE%");
            sb.AppendLine("  echo   [진단] 타겟 전체 파일:");
            sb.AppendLine("  dir \"%TARGET_DIR%\" /S /B");
            sb.AppendLine("  echo   [진단] 로그 파일:");
            sb.AppendLine("  type \"%LOG_FILE%\"");
            sb.AppendLine("  pause");
            sb.AppendLine(")");
            sb.AppendLine("echo [7단계] 완료");
            sb.AppendLine("");

            // 🔟 정리
            sb.AppendLine("echo [8단계] 정리 중...");
            sb.AppendLine("timeout /t 3 /nobreak > nul");
            sb.AppendLine("echo   배치 파일 제거: %~f0");
            sb.AppendLine("echo   PS 스크립트 제거: %PS_SCRIPT%");
            sb.AppendLine("del /F /Q \"%PS_SCRIPT%\" 2>nul");
            sb.AppendLine("del /F /Q \"%~f0\" 2>nul");
            sb.AppendLine("exit /b 0");

            File.WriteAllText(batchPath, sb.ToString(), Encoding.UTF8);
            Console.WriteLine($"[배치 파일] 생성: {batchPath}");
            Console.WriteLine($"[타겟] {targetPath}");
            Console.WriteLine($"[소스] {sourcePath}");
        }

        /// <summary>
        /// 파일 다운로드
        /// </summary>
        private async Task DownloadFile(string url, string destinationPath, CancellationToken cancellationToken)
        {
            using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var httpStream = await response.Content.ReadAsStreamAsync())
                {
                    await httpStream.CopyToAsync(fileStream, 8192, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 파일 해시 검증
        /// </summary>
        private async Task<bool> VerifyFileHash(string filePath, string expectedHash)
        {
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                Console.WriteLine("[해시 검증] expectedHash가 비어있어서 검증 생략");
                return true;
            }

            using (var sha256 = SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
                var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                bool isValid = actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);

                Console.WriteLine($"[해시 검증] Expected: {expectedHash}");
                Console.WriteLine($"[해시 검증] Actual:   {actualHash}");
                Console.WriteLine($"[해시 검증] 결과: {(isValid ? "통과" : "실패")}");

                return isValid;
            }
        }

        /// <summary>
        /// 디렉토리 복사
        /// </summary>
        private void CopyDirectory(string sourceDir, string targetDir, bool overwrite)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir, overwrite);
            }
        }

        /// <summary>
        /// 현재 게임 버전 가져오기
        /// </summary>
        public string GetCurrentGameVersion()
        {
            try
            {
                var localVersion = LocalVersionInfo.Load();
                return localVersion.GameVersion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[버전 조회] 실패: {ex.Message}");
                return "0.0.1";
            }
        }
    }
}