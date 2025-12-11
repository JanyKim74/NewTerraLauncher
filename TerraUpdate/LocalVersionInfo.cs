using Newtonsoft.Json;
using System;
using System.IO;

namespace Updater
{
    /// <summary>
    /// 로컬 PC에 저장된 현재 버전 정보
    /// 파일 위치: local_version.json
    /// </summary>
    public class LocalVersionInfo
    {
        [JsonProperty("LauncherVersion")]
        public string LauncherVersion { get; set; } = "0.0.1";

        [JsonProperty("GameVersion")]
        public string GameVersion { get; set; } = "0.0.1";

        [JsonProperty("LastUpdateDate")]
        public DateTime LastUpdateDate { get; set; } = DateTime.Now;

        private static readonly string LocalVersionFilePath = "local_version.json";

        /// <summary>
        /// ⭐ 실제 저장될 경로 (부모 디렉토리)
        /// 모든 런처 버전이 같은 파일을 공유하도록 함
        /// </summary>
        private static string GetVersionFilePath()
        {
            // 부모 폴더
            //string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            //string parentDir = Path.GetDirectoryName(currentDir.TrimEnd('\\', '/'));
            //return Path.Combine(parentDir, LocalVersionFilePath);
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(currentDir, LocalVersionFilePath);  // ✅ 같은 폴더
        }

        /// <summary>
        /// 로컬 버전 정보 로드
        /// </summary>
        public static LocalVersionInfo Load()
        {
            try
            {
                string filePath = GetVersionFilePath();

                Console.WriteLine($"[버전 로드] 경로: {filePath}");

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Console.WriteLine($"[버전 로드] JSON 내용: {json}");

                    var versionInfo = JsonConvert.DeserializeObject<LocalVersionInfo>(json);
                    Console.WriteLine($"[버전 로드] 성공 - 런처 {versionInfo.LauncherVersion}, 게임 {versionInfo.GameVersion}");
                    return versionInfo;
                }
                else
                {
                    Console.WriteLine("[버전 로드] local_version.json 파일이 없습니다. 기본값 사용.");
                    // 파일이 없으면 기본값으로 생성
                    var defaultVersion = new LocalVersionInfo();
                    defaultVersion.Save();
                    return defaultVersion;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[버전 로드] 실패: {ex.Message}");
                Console.WriteLine($"[버전 로드] 스택트레이스: {ex.StackTrace}");
                return new LocalVersionInfo();
            }
        }

        /// <summary>
        /// 로컬 버전 정보 저장
        /// </summary>
        public void Save()
        {
            try
            {
                string filePath = GetVersionFilePath();

                Console.WriteLine($"[버전 저장] 경로: {filePath}");

                // ⭐ 디렉토리가 없으면 생성
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[버전 저장] 디렉토리 생성: {directory}");
                }

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                Console.WriteLine($"[버전 저장] 저장 전 내용: {json}");

                File.WriteAllText(filePath, json);

                // 저장 확인
                if (File.Exists(filePath))
                {
                    string verifyJson = File.ReadAllText(filePath);
                    Console.WriteLine($"[버전 저장] 저장 후 확인 내용: {verifyJson}");
                    Console.WriteLine($"[버전 저장] 성공 - 런처 {LauncherVersion}, 게임 {GameVersion}, 시간 {LastUpdateDate:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    Console.WriteLine($"[버전 저장] 경고: 파일이 생성되지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[버전 저장] 실패: {ex.Message}");
                Console.WriteLine($"[버전 저장] 스택트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 런처 버전 업데이트
        /// </summary>
        public void UpdateLauncherVersion(string newVersion)
        {
            Console.WriteLine($"[버전 업데이트] 런처 버전 업데이트 시작: {LauncherVersion} → {newVersion}");

            LauncherVersion = newVersion;
            LastUpdateDate = DateTime.Now;

            Console.WriteLine($"[버전 업데이트] 메모리에 적용: {LauncherVersion}");

            Save();

            Console.WriteLine($"[버전 업데이트] 런처 버전 업데이트 완료");
        }

        /// <summary>
        /// 게임 버전 업데이트
        /// </summary>
        public void UpdateGameVersion(string newVersion)
        {
            Console.WriteLine($"[버전 업데이트] 게임 버전 업데이트 시작: {GameVersion} → {newVersion}");

            GameVersion = newVersion;
            LastUpdateDate = DateTime.Now;

            Console.WriteLine($"[버전 업데이트] 메모리에 적용: {GameVersion}");

            Save();

            Console.WriteLine($"[버전 업데이트] 게임 버전 업데이트 완료");
        }
    }
}