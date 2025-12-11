using Newtonsoft.Json;
using System;
using System.IO;

namespace Updater
{
    /// <summary>
    /// 게임의 SystemConfig.json 파일 파싱 클래스
    /// 위치: Game/Saved/SystemConfig.json
    /// </summary>
    public class SystemConfig
    {
        [JsonProperty("ComPort")]
        public int ComPort { get; set; } = 7;

        [JsonProperty("BaudRate")]
        public int BaudRate { get; set; } = 9600;

        [JsonProperty("AutoTeeEnabled")]
        public bool AutoTeeEnabled { get; set; } = true;

        [JsonProperty("KeyRepeatInterval")]
        public double KeyRepeatInterval { get; set; } = 0.15;

        [JsonProperty("KeyRepeatDelay")]
        public double KeyRepeatDelay { get; set; } = 0.15;

        /// <summary>
        /// SystemConfig.json 파일 로드
        /// </summary>
        public static SystemConfig Load(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<SystemConfig>(json);
                    Console.WriteLine($"SystemConfig 로드 성공 - ComPort: {config.ComPort}, BaudRate: {config.BaudRate}");
                    return config;
                }
                else
                {
                    Console.WriteLine($"SystemConfig 파일을 찾을 수 없습니다: {configPath}");
                    return new SystemConfig(); // 기본값 반환
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SystemConfig 로드 실패: {ex.Message}");
                return new SystemConfig(); // 기본값 반환
            }
        }

        /// <summary>
        /// SystemConfig.json 파일 저장
        /// </summary>
        public void Save(string configPath)
        {
            try
            {
                // 디렉토리 생성
                string directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(configPath, json);
                Console.WriteLine($"SystemConfig 저장 성공: {configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SystemConfig 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// COM 포트 이름 가져오기
        /// </summary>
        public string GetComPortName()
        {
            return $"COM{ComPort}";
        }
    }
}
