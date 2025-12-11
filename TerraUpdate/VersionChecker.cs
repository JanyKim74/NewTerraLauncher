using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Updater
{
    /// <summary>
    /// 버전 체크 및 업데이트 관리
    /// </summary>
    public class VersionChecker
    {
        private readonly HttpClient httpClient;
        private readonly string serverUrl;
        private LocalVersionInfo localVersion;

        public VersionChecker(HttpClient client, string versionJsonUrl)
        {
            httpClient = client;
            serverUrl = versionJsonUrl;
        }

        /// <summary>
        /// 버전 체크 수행
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdates()
        {
            try
            {
                // 1. 로컬 버전 로드
                localVersion = LocalVersionInfo.Load();
                Console.WriteLine("=== 로컬 버전 ===");
                Console.WriteLine($"런처: {localVersion.LauncherVersion}");
                Console.WriteLine($"게임: {localVersion.GameVersion}");

                // 2. 서버에서 최신 버전 가져오기
                Console.WriteLine("=== 서버에서 최신 버전 확인 중... ===");
                string response = await httpClient.GetStringAsync(serverUrl);
                var serverVersion = JsonConvert.DeserializeObject<ServerVersionInfo>(response);

                Console.WriteLine("=== 서버 버전 ===");
                Console.WriteLine($"런처: {serverVersion.LauncherVersion}");
                Console.WriteLine($"게임: {serverVersion.GameVersion}");

                // 3. 비교
                var result = serverVersion.CompareWithLocal(localVersion);

                Console.WriteLine("=== 비교 결과 ===");
                Console.WriteLine($"런처 업데이트 필요: {result.NeedsLauncherUpdate}");
                Console.WriteLine($"게임 업데이트 필요: {result.NeedsGameUpdate}");
                Console.WriteLine($"상태: {result.GetStatusMessage()}");

                return result;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"서버 연결 실패: {ex.Message}");
                throw new Exception("서버에 연결할 수 없습니다.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"버전 체크 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 런처 업데이트 완료 후 버전 업데이트
        /// </summary>
        public void UpdateLauncherVersionAfterUpdate(string newVersion)
        {
            if (localVersion != null)
            {
                localVersion.UpdateLauncherVersion(newVersion);
                Console.WriteLine($"런처 버전 업데이트 완료: {newVersion}");
            }
        }

        /// <summary>
        /// 게임 업데이트 완료 후 버전 업데이트
        /// </summary>
        public void UpdateGameVersionAfterUpdate(string newVersion)
        {
            if (localVersion != null)
            {
                localVersion.UpdateGameVersion(newVersion);
                Console.WriteLine($"게임 버전 업데이트 완료: {newVersion}");
            }
        }

        /// <summary>
        /// 로컬 버전 정보 가져오기
        /// </summary>
        public LocalVersionInfo GetLocalVersion()
        {
            return localVersion ?? LocalVersionInfo.Load();
        }
    }
}
