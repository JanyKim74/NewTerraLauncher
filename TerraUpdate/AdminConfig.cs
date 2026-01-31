using Newtonsoft.Json;
using System;
using System.IO;

namespace Updater
{
    /// <summary>
    /// 관리자 설정 파일 (adminConfig.json)
    /// 위치: ParkDay\Saved\adminConfig.json
    /// </summary>
    public class AdminConfig
    {
        /// <summary>
        /// 관리자 비밀번호 (5자리)
        /// </summary>
        [JsonProperty("AdminPassword")]
        public string AdminPassword { get; set; } = "12345";

        /// <summary>
        /// 연습장 시간 설정 (분)
        /// </summary>
        [JsonProperty("PracticeTimeMinutes")]
        public int PracticeTimeMinutes { get; set; } = 60;

        /// <summary>
        /// 장비 ID
        /// </summary>
        [JsonProperty("DeviceId")]
        public string DeviceId { get; set; } = "0";

        /// <summary>
        /// 룸 번호
        /// </summary>
        [JsonProperty("RoomNumber")]
        public string RoomNumber { get; set; } = "0";

        /// <summary>
        /// 시선조절 설정 (-10 ~ +10)
        /// </summary>
        [JsonProperty("GazeControl")]
        public int GazeControl { get; set; } = 0;

        /// <summary>
        /// 공 색상 (White, Yellow, Green, Blue, Brown)
        /// </summary>
        [JsonProperty("BallColor")]
        public string BallColor { get; set; } = "Brown";

        /// <summary>
        /// ⭐ NEW: 비밀번호 사용 여부 (0=미적용, 1=적용)
        /// </summary>
        [JsonProperty("UsePassword")]
        public int UsePassword { get; set; } = 1;  // 기본값: 1 (적용)

        /// <summary>
        /// 하드웨어 상태
        /// </summary>
        [JsonProperty("HardwareStatus")]
        public HardwareStatus HardwareStatus { get; set; } = new HardwareStatus();
    }

    /// <summary>
    /// 하드웨어 상태 정보
    /// </summary>
    public class HardwareStatus
    {
        [JsonProperty("MotionCAM")]
        public bool MotionCAM { get; set; } = true;

        [JsonProperty("AutoTee")]
        public bool AutoTee { get; set; } = false;

        [JsonProperty("Sensor")]
        public bool Sensor { get; set; } = false;

        [JsonProperty("Projector")]
        public bool Projector { get; set; } = true;

        [JsonProperty("Kiosk")]
        public bool Kiosk { get; set; } = true;
    }

    /// <summary>
    /// AdminConfig 관리 클래스
    /// </summary>
    public static class AdminConfigManager
    {
        private static readonly string AdminConfigPath = "ParkDay\\Saved\\adminConfig.json";

        /// <summary>
        /// adminConfig.json 로드
        /// </summary>
        public static AdminConfig Load()
        {
            try
            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    AdminConfigPath);

                Console.WriteLine($"[AdminConfig] 로드 경로: {filePath}");

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var config = JsonConvert.DeserializeObject<AdminConfig>(json);
                    Console.WriteLine($"[AdminConfig] 로드 성공");
                    Console.WriteLine($"  - 비밀번호: {new string('*', config.AdminPassword.Length)}");
                    Console.WriteLine($"  - 연습장 시간: {config.PracticeTimeMinutes}분");
                    Console.WriteLine($"  - 장비 ID: {config.DeviceId}");
                    Console.WriteLine($"  - 룸 번호: {config.RoomNumber}");
                    Console.WriteLine($"  - 비밀번호 적용: {(config.UsePassword == 1 ? "✅ 적용" : "❌ 미적용")}");
                    return config;
                }
                else
                {
                    Console.WriteLine($"[AdminConfig] 파일이 없습니다. 기본값 생성.");
                    var defaultConfig = new AdminConfig();
                    defaultConfig.Save();
                    return defaultConfig;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminConfig] 로드 실패: {ex.Message}");
                return new AdminConfig();
            }
        }

        /// <summary>
        /// adminConfig.json 저장
        /// </summary>
        public static void Save(AdminConfig config)
        {
            try
            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    AdminConfigPath);

                // 디렉토리 생성
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"[AdminConfig] 디렉토리 생성: {directory}");
                }

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Console.WriteLine($"[AdminConfig] 저장 성공: {filePath}");
                Console.WriteLine($"  - 비밀번호: {new string('*', config.AdminPassword.Length)}");
                Console.WriteLine($"  - 연습장 시간: {config.PracticeTimeMinutes}분");
                Console.WriteLine($"  - 장비 ID: {config.DeviceId}");
                Console.WriteLine($"  - 룸 번호: {config.RoomNumber}");
                Console.WriteLine($"  - 비밀번호 적용: {(config.UsePassword == 1 ? "✅ 적용" : "❌ 미적용")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminConfig] 저장 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 비밀번호 검증
        /// </summary>
        public static bool ValidatePassword(string inputPassword)
        {
            var config = Load();
            // UsePassword = 1일 때만 비밀번호 검증
            if (config.UsePassword == 0)
            {
                Console.WriteLine($"[AdminConfig] 비밀번호 검증 생략 (UsePassword = 0)");
                return true;
            }
            return config.AdminPassword == inputPassword;
        }

        /// <summary>
        /// 비밀번호 변경
        /// </summary>
        public static bool ChangePassword(string oldPassword, string newPassword)
        {
            var config = Load();

            if (config.AdminPassword != oldPassword)
            {
                Console.WriteLine($"[AdminConfig] 비밀번호 변경 실패: 이전 비밀번호 불일치");
                return false;
            }

            if (newPassword.Length != 5)
            {
                Console.WriteLine($"[AdminConfig] 비밀번호 변경 실패: 길이가 5자리가 아님");
                return false;
            }

            config.AdminPassword = newPassword;
            Save(config);

            Console.WriteLine($"[AdminConfig] 비밀번호 변경 성공");
            return true;
        }
    }

    /// <summary>
    /// AdminConfig 확장 메서드
    /// </summary>
    public static class AdminConfigExtensions
    {
        public static void Save(this AdminConfig config)
        {
            AdminConfigManager.Save(config);
        }
    }
}