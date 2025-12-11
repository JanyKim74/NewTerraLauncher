using Newtonsoft.Json;
using System;

namespace Updater
{
    /// <summary>
    /// 서버에서 제공하는 최신 버전 정보
    /// 서버 URL: /api/version.json
    /// </summary>
    public class ServerVersionInfo
    {
        [JsonProperty("LauncherVersion")]
        public string LauncherVersion { get; set; }

        [JsonProperty("LauncherDownloadUrl")]
        public string LauncherDownloadUrl { get; set; }

        [JsonProperty("GameVersion")]
        public string GameVersion { get; set; }

        [JsonProperty("GameDownloadUrl")]
        public string GameDownloadUrl { get; set; }

        [JsonProperty("ReleaseDate")]
        public DateTime ReleaseDate { get; set; }

        [JsonProperty("ReleaseNotes")]
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// 로컬 버전과 비교하여 업데이트 필요 여부 확인
        /// </summary>
        public UpdateCheckResult CompareWithLocal(LocalVersionInfo localVersion)
        {
            var result = new UpdateCheckResult();

            // 런처 업데이트 필요 확인
            result.NeedsLauncherUpdate = CompareVersions(
                localVersion.LauncherVersion, 
                this.LauncherVersion) < 0;

            // 게임 업데이트 필요 확인
            result.NeedsGameUpdate = CompareVersions(
                localVersion.GameVersion, 
                this.GameVersion) < 0;

            result.LocalLauncherVersion = localVersion.LauncherVersion;
            result.ServerLauncherVersion = this.LauncherVersion;
            result.LocalGameVersion = localVersion.GameVersion;
            result.ServerGameVersion = this.GameVersion;

            return result;
        }

        /// <summary>
        /// 버전 비교
        /// </summary>
        /// <returns>-1: v1 < v2 (업데이트 필요), 0: 같음, 1: v1 > v2</returns>
        private int CompareVersions(string version1, string version2)
        {
            if (string.IsNullOrWhiteSpace(version1) || string.IsNullOrWhiteSpace(version2))
                return 0;

            try
            {
                var v1 = new Version(version1);
                var v2 = new Version(version2);
                return v1.CompareTo(v2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"버전 비교 실패: {ex.Message}");
                return string.Compare(version1, version2, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// 버전 비교 결과
    /// </summary>
    public class UpdateCheckResult
    {
        public bool NeedsLauncherUpdate { get; set; }
        public bool NeedsGameUpdate { get; set; }
        public string LocalLauncherVersion { get; set; }
        public string ServerLauncherVersion { get; set; }
        public string LocalGameVersion { get; set; }
        public string ServerGameVersion { get; set; }

        /// <summary>
        /// 어떤 업데이트든 필요한지 확인
        /// </summary>
        public bool NeedsAnyUpdate()
        {
            return NeedsLauncherUpdate || NeedsGameUpdate;
        }

        /// <summary>
        /// 업데이트 상태 메시지
        /// </summary>
        public string GetStatusMessage()
        {
            if (NeedsLauncherUpdate && NeedsGameUpdate)
                return "런처+게임 업데이트";
            else if (NeedsLauncherUpdate)
                return "런처 업데이트";
            else if (NeedsGameUpdate)
                return "게임 업데이트";
            else
                return "최신";
        }
    }
}
