using Newtonsoft.Json;
using System;
using System.IO;

namespace Updater
{
    /// <summary>
    /// 런처 설정 파일 관리
    /// ⭐ 절대 경로 사용으로 launcher_config.json 중복 생성 문제 해결
    /// </summary>
    public class LauncherConfig
    {
        public string ServerUrl { get; set; } = "https://admin.terraparkgolf.net";

        /// <summary>
        /// game_update.json URL (통합 업데이트 정보)
        /// </summary>
        public string GameUpdateJsonUrl { get; set; } = "/api/game/version";

        /// <summary>
        /// 채널 타입 (STABLE, BETA) - 기본값: STABLE
        /// Channel 속성이 없으면 STABLE로 간주
        /// </summary>
        public string Channel { get; set; } = "STABLE";

        /// <summary>
        /// [Deprecated] 이전 버전 호환성을 위한 속성
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Shipping { get; set; }

        public int SerialPortTimeout { get; set; } = 3000;
        public int SerialBaudRate { get; set; } = 9600;
        public string GameExecutablePath { get; set; } = "ParkDay/Binaries/Win64/ParkDay.exe";
        public string SystemConfigPath { get; set; } = "Game/Saved/SystemConfig.json";

        private static LauncherConfig _instance;

        /// <summary>
        /// ⭐ 절대 경로를 동적으로 구성하는 메서드
        /// AppDomain.CurrentDomain.BaseDirectory를 사용하여
        /// 항상 런처 폴더의 launcher_config.json을 가리킴
        /// </summary>
        private static string GetConfigFilePath()
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(basePath, "launcher_config.json");
            return configPath;
        }

        /// <summary>
        /// 설정 파일 로드
        /// </summary>
        public static LauncherConfig Load()
        {
            if (_instance != null)
                return _instance;

            try
            {
                string configPath = GetConfigFilePath();
                Console.WriteLine($"[설정] 설정 파일 경로: {configPath}");

                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    _instance = JsonConvert.DeserializeObject<LauncherConfig>(json);

                    // Channel 속성이 null이거나 비어있으면 기본값(STABLE) 설정
                    if (string.IsNullOrWhiteSpace(_instance.Channel))
                    {
                        _instance.Channel = "STABLE";
                        Console.WriteLine("[설정] Channel 속성이 없어서 기본값(STABLE)으로 설정합니다.");
                    }

                    Console.WriteLine($"[설정] 파일 로드: {configPath}, Channel: {_instance.Channel}");
                }
                else
                {
                    // 기본 설정 생성
                    Console.WriteLine($"[설정] 파일을 찾을 수 없습니다: {configPath}");
                    _instance = new LauncherConfig();
                    _instance.Save();
                    Console.WriteLine($"[설정] 기본 설정 파일 생성: {configPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[설정] 파일 로드 실패: {ex.Message}");
                Console.WriteLine($"[설정] 스택 트레이스: {ex.StackTrace}");
                _instance = new LauncherConfig();
            }

            return _instance;
        }

        /// <summary>
        /// 설정 파일 저장
        /// </summary>
        public void Save()
        {
            try
            {
                string configPath = GetConfigFilePath();

                // 디렉토리 생성 (안전장치)
                string directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[설정] 디렉토리 생성: {directory}");
                }

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(configPath, json);
                Console.WriteLine($"[설정] 파일 저장: {configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[설정] 파일 저장 실패: {ex.Message}");
                Console.WriteLine($"[설정] 스택 트레이스: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 전체 URL 생성
        /// </summary>
        public string GetFullUrl(string endpoint)
        {
            return ServerUrl.TrimEnd('/') + endpoint;
        }

        /// <summary>
        /// 현재 채널에 맞는 API URL 가져오기
        /// </summary>
        public string GetVersionApiUrl()
        {
            // Channel이 BETA면 모든 버전 가져오기, STABLE이면 STABLE만 가져오기
            string channel = string.IsNullOrWhiteSpace(Channel) ? "STABLE" : Channel.ToUpper();
            return $"{GameUpdateJsonUrl}?channel={channel}";
        }
    }
}
