using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Updater
{
    /// <summary>
    /// 서버에서 제공하는 단일 버전 정보
    /// API: https://admin.terraparkgolf.net/api/game/version?channel=BETA
    /// </summary>
    public class GameUpdateInfo
    {
        /// <summary>
        /// 런처 버전
        /// </summary>
        [JsonProperty("launcherVersion")]
        public string LauncherIndex { get; set; }

        /// <summary>
        /// 런처 다운로드 URL
        /// </summary>
        [JsonProperty("launcherUrl")]
        public string LauncherUrl { get; set; }

        /// <summary>
        /// 런처 파일 SHA256 해시 (무결성 검증용)
        /// </summary>
        [JsonProperty("launcherHash")]
        public string LauncherHash { get; set; }

        /// <summary>
        /// 게임 버전
        /// </summary>
        [JsonProperty("gameVersion")]
        public string UpdateIndex { get; set; }

        /// <summary>
        /// 게임 업데이트 파일 다운로드 URL
        /// </summary>
        [JsonProperty("gameUrl")]
        public string UpdateFileUrl { get; set; }

        /// <summary>
        /// 게임 파일 SHA256 해시 (무결성 검증용)
        /// </summary>
        [JsonProperty("gameHash")]
        public string UpdateFileHash { get; set; }

        /// <summary>
        /// 업데이트 릴리즈 노트
        /// </summary>
        [JsonProperty("releaseNotes")]
        public string ReleaseNotes { get; set; }

        /// <summary>
        /// 릴리즈 날짜
        /// </summary>
        [JsonProperty("releaseDate")]
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// 채널 타입 (STABLE, BETA)
        /// </summary>
        [JsonProperty("channel")]
        public string Shipping { get; set; }

        /// <summary>
        /// 현재 설치된 런처 버전 (로컬) - 로컬에서 설정
        /// </summary>
        [JsonIgnore]
        public string CurrentLauncherIndex { get; set; }

        /// <summary>
        /// 현재 설치된 게임 버전 (로컬) - 로컬에서 설정
        /// </summary>
        [JsonIgnore]
        public string CurrentUpdateIndex { get; set; }

        /// <summary>
        /// 런처 업데이트가 필요한지 확인
        /// </summary>
        public bool NeedsLauncherUpdate()
        {
            if (string.IsNullOrEmpty(CurrentLauncherIndex) || string.IsNullOrEmpty(LauncherIndex))
                return false;

            return CompareVersions(CurrentLauncherIndex, LauncherIndex) < 0;
        }

        /// <summary>
        /// 게임 업데이트가 필요한지 확인
        /// </summary>
        public bool NeedsGameUpdate()
        {
            if (string.IsNullOrEmpty(CurrentUpdateIndex) || string.IsNullOrEmpty(UpdateIndex))
                return false;

            return CompareVersions(CurrentUpdateIndex, UpdateIndex) < 0;
        }

        /// <summary>
        /// 어떤 업데이트든 필요한지 확인
        /// </summary>
        public bool NeedsAnyUpdate()
        {
            return NeedsLauncherUpdate() || NeedsGameUpdate();
        }

        /// <summary>
        /// 업데이트 타입 가져오기
        /// </summary>
        public string GetUpdateType()
        {
            if (NeedsLauncherUpdate() && NeedsGameUpdate())
                return "런처+게임";
            else if (NeedsGameUpdate())
                return "게임";
            else if (NeedsLauncherUpdate())
                return "런처";
            else
                return "최신";
        }

        /// <summary>
        /// 버전 비교 (예: "0.0.1" vs "0.0.2")
        /// </summary>
        /// <returns>-1: version1 < version2, 0: 같음, 1: version1 > version2</returns>
        private int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1 = new Version(version1);
                var v2 = new Version(version2);
                return v1.CompareTo(v2);
            }
            catch
            {
                // 버전 파싱 실패 시 문자열 비교
                return string.Compare(version1, version2, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// 서버에서 받은 버전 리스트를 처리하는 헬퍼 클래스
    /// </summary>
    public class GameUpdateInfoList
    {
        /// <summary>
        /// 버전 정보 리스트
        /// </summary>
        public List<GameUpdateInfo> Versions { get; set; }

        /// <summary>
        /// 현재 로컬 런처 버전
        /// </summary>
        public string CurrentLauncherVersion { get; set; }

        /// <summary>
        /// 현재 로컬 게임 버전
        /// </summary>
        public string CurrentGameVersion { get; set; }

        /// <summary>
        /// 현재 설정된 채널
        /// </summary>
        public string CurrentChannel { get; set; }

        public GameUpdateInfoList()
        {
            Versions = new List<GameUpdateInfo>();
        }

        /// <summary>
        /// 채널에 맞는 버전 필터링
        /// BETA 채널: STABLE + BETA 모두 포함
        /// STABLE 채널: STABLE만 포함
        /// </summary>
        private List<GameUpdateInfo> FilterByChannel()
        {
            string channel = string.IsNullOrWhiteSpace(CurrentChannel) ? "STABLE" : CurrentChannel.ToUpper();
            
            if (channel == "BETA")
            {
                // BETA 채널: STABLE과 BETA 모두 포함
                return Versions.Where(v => 
                    string.Equals(v.Shipping, "STABLE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(v.Shipping, "BETA", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                // STABLE 채널: STABLE만 포함
                return Versions.Where(v => 
                    string.Equals(v.Shipping, "STABLE", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>
        /// 현재 버전보다 높은 게임 업데이트 리스트 가져오기 (오름차순 정렬)
        /// 채널 필터링 적용
        /// </summary>
        public List<GameUpdateInfo> GetPendingGameUpdates()
        {
            if (string.IsNullOrEmpty(CurrentGameVersion))
                return new List<GameUpdateInfo>();

            var filteredVersions = FilterByChannel();
            
            var pendingUpdates = filteredVersions
                .Where(v => !string.IsNullOrEmpty(v.UpdateIndex) &&
                            CompareVersions(CurrentGameVersion, v.UpdateIndex) < 0)
                .OrderBy(v => new Version(v.UpdateIndex))
                .ToList();

            Console.WriteLine($"[채널 필터링] 현재 채널: {CurrentChannel ?? "STABLE"}");
            Console.WriteLine($"[채널 필터링] 전체 버전 수: {Versions.Count}");
            Console.WriteLine($"[채널 필터링] 필터링된 버전 수: {filteredVersions.Count}");
            Console.WriteLine($"[채널 필터링] 대기 중인 게임 업데이트 수: {pendingUpdates.Count}");

            return pendingUpdates;
        }

        /// <summary>
        /// 현재 버전보다 높은 런처 업데이트 리스트 가져오기 (오름차순 정렬)
        /// 채널 필터링 적용
        /// </summary>
        public List<GameUpdateInfo> GetPendingLauncherUpdates()
        {
            if (string.IsNullOrEmpty(CurrentLauncherVersion))
                return new List<GameUpdateInfo>();

            var filteredVersions = FilterByChannel();
            
            var pendingUpdates = filteredVersions
                .Where(v => !string.IsNullOrEmpty(v.LauncherIndex) &&
                            CompareVersions(CurrentLauncherVersion, v.LauncherIndex) < 0)
                .OrderBy(v => new Version(v.LauncherIndex))
                .ToList();

            Console.WriteLine($"[채널 필터링] 대기 중인 런처 업데이트 수: {pendingUpdates.Count}");

            return pendingUpdates;
        }

        /// <summary>
        /// 최신 게임 버전 정보 가져오기
        /// 채널 필터링 적용
        /// </summary>
        public GameUpdateInfo GetLatestGameVersion()
        {
            var filteredVersions = FilterByChannel();
            
            return filteredVersions
                .Where(v => !string.IsNullOrEmpty(v.UpdateIndex))
                .OrderByDescending(v => new Version(v.UpdateIndex))
                .FirstOrDefault();
        }

        /// <summary>
        /// 최신 런처 버전 정보 가져오기
        /// 채널 필터링 적용
        /// </summary>
        public GameUpdateInfo GetLatestLauncherVersion()
        {
            var filteredVersions = FilterByChannel();
            
            return filteredVersions
                .Where(v => !string.IsNullOrEmpty(v.LauncherIndex))
                .OrderByDescending(v => new Version(v.LauncherIndex))
                .FirstOrDefault();
        }

        /// <summary>
        /// 대기 중인 업데이트가 있는지 확인
        /// </summary>
        public bool HasPendingUpdates()
        {
            return GetPendingGameUpdates().Any() || GetPendingLauncherUpdates().Any();
        }

        /// <summary>
        /// 업데이트 요약 정보
        /// </summary>
        public string GetUpdateSummary()
        {
            var gameUpdates = GetPendingGameUpdates();
            var launcherUpdates = GetPendingLauncherUpdates();

            if (gameUpdates.Any() && launcherUpdates.Any())
                return $"런처 {launcherUpdates.Count}개 + 게임 {gameUpdates.Count}개 업데이트";
            else if (gameUpdates.Any())
                return $"게임 {gameUpdates.Count}개 업데이트";
            else if (launcherUpdates.Any())
                return $"런처 {launcherUpdates.Count}개 업데이트";
            else
                return "최신 버전";
        }

        /// <summary>
        /// 모든 릴리즈 노트 통합
        /// </summary>
        public string GetCombinedReleaseNotes()
        {
            var gameUpdates = GetPendingGameUpdates();
            var launcherUpdates = GetPendingLauncherUpdates();

            var notes = new List<string>();

            if (gameUpdates.Any())
            {
                notes.Add("=== 게임 업데이트 ===");
                foreach (var update in gameUpdates)
                {
                    if (!string.IsNullOrWhiteSpace(update.ReleaseNotes))
                    {
                        notes.Add($"[v{update.UpdateIndex}] ({update.Shipping})");
                        notes.Add(update.ReleaseNotes);
                        notes.Add("");
                    }
                }
            }

            if (launcherUpdates.Any())
            {
                notes.Add("=== 런처 업데이트 ===");
                foreach (var update in launcherUpdates)
                {
                    if (!string.IsNullOrWhiteSpace(update.ReleaseNotes))
                    {
                        notes.Add($"[v{update.LauncherIndex}] ({update.Shipping})");
                        notes.Add(update.ReleaseNotes);
                        notes.Add("");
                    }
                }
            }

            return string.Join(Environment.NewLine, notes).Trim();
        }

        /// <summary>
        /// 버전 비교
        /// </summary>
        private int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1 = new Version(version1);
                var v2 = new Version(version2);
                return v1.CompareTo(v2);
            }
            catch
            {
                return string.Compare(version1, version2, StringComparison.Ordinal);
            }
        }
    }
}
