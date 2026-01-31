using Newtonsoft.Json;
using System;
using System.IO;

namespace Updater
{
    /// <summary>
    /// defaultGameData.json 파일의 GameOptions 구조
    /// ⭐ 멀리건(Mulligan) 값 매핑:
    /// 
    /// 로드시 (파일 → UI):
    ///   - "Mulligan_Count": 0  → UI값: 0  (무제한)
    ///   - "Mulligan_Count": 1  → UI값: 1  (1개)
    ///   - "Mulligan_Count": 3  → UI값: 2  (3개)
    ///   - "Mulligan_Count": -1 → UI값: 3  (5개)
    ///
    /// 저장시 (UI → 파일):
    ///   - UI값: 0 → "Mulligan_Count": 0  (무제한)
    ///   - UI값: 1 → "Mulligan_Count": 1  (1개)
    ///   - UI값: 2 → "Mulligan_Count": 3  (3개)
    ///   - UI값: 3 → "Mulligan_Count": -1 (5개)
    /// </summary>
    public class DefaultGameData
    {
        [JsonProperty("GameOptions")]
        public GameOptions GameOptions { get; set; } = new GameOptions();
    }

    public class GameOptions
    {
        // 기존 필드 (호환성 유지)
        [JsonProperty("SelectCourse")]
        public int SelectCourse { get; set; } = 2;

        [JsonProperty("ContinuePutting")]
        public int ContinuePutting { get; set; } = 0;

        [JsonProperty("Holecup_Position")]
        public int HolecupPosition { get; set; } = 0;

        /// <summary>
        /// ⭐ 멀리건 설정 (실제 파일에 저장되는 값)
        /// 0 = 무제한, 1 = 1개, 3 = 3개, -1 = 5개
        /// </summary>
        [JsonProperty("Mulligan_Count")]
        public int Mulligan_Count { get; set; } = 0;  // 기본값: 무제한

        [JsonProperty("Concede_Distance")]
        public int Concede_Distance { get; set; } = 1;

        [JsonProperty("Green_Speed")]
        public int Green_Speed { get; set; } = 1;

        [JsonProperty("PracticeBall")]
        public int PracticeBall { get; set; } = 0;

        [JsonProperty("Movie_SaveCount")]
        public int Movie_SaveCount { get; set; } = 0;

        [JsonProperty("Camera_Mode")]
        public int Camera_Mode { get; set; } = 0;

        [JsonProperty("GameType")]
        public int GameType { get; set; } = 1;

        [JsonProperty("SwingMotion")]
        public int SwingMotion { get; set; } = 0;

        [JsonProperty("Difficulty")]
        public int Difficulty { get; set; } = 1;

        [JsonProperty("PracticeTimeLimit")]
        public int PracticeTimeLimit { get; set; } = 10;

        [JsonProperty("PinPosition")]
        public int PinPosition { get; set; } = 1;
    }

    /// <summary>
    /// GameOptions 관리 클래스
    /// </summary>
    public class GameOptionsManager
    {
        private static readonly string DefaultGameDataPath = "ParkDay\\Saved\\defaultGameData.json";

        /// <summary>
        /// ⭐ 멀리건: 파일값 → UI값으로 변환 (로드시)
        /// 파일에서 읽은 Mulligan_Count → RadioButton Tag값으로 변환
        /// </summary>
        public static int ConvertMulliganFileToUI(int fileValue)
        {
            switch (fileValue)
            {
                case 0: return 0;    // 파일: 0   → UI: 0 (무제한)
                case 1: return 1;    // 파일: 1   → UI: 1 (1개)
                case 3: return 2;    // 파일: 3   → UI: 2 (3개)
                case -1: return 3;    // 파일: -1  → UI: 3 (5개)
                default: return 0;    // 기본값: 무제한
            }
        }

        /// <summary>
        /// ⭐ 멀리건: UI값 → 파일값으로 변환 (저장시)
        /// RadioButton Tag값 → 파일의 Mulligan_Count로 변환
        /// </summary>
        public static int ConvertMulliganUIToFile(int uiValue)
        {
            switch (uiValue)
            {
                case 0: return 0;    // UI: 0 → 파일: 0   (무제한)
                case 1: return 1;    // UI: 1 → 파일: 1   (1개)
                case 2: return 3;    // UI: 2 → 파일: 3   (3개)
                case 3: return -1;   // UI: 3 → 파일: -1  (5개)
                default: return 0;    // 기본값: 무제한
            }
        }

        /// <summary>
        /// defaultGameData.json 로드
        /// </summary>
        public static DefaultGameData Load()
        {
            try
            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    DefaultGameDataPath);

                Console.WriteLine($"[GameOptionsManager] 로드 시도");
                Console.WriteLine($"[GameOptionsManager] 경로: {filePath}");

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Console.WriteLine($"[GameOptionsManager] 파일 크기: {json.Length}");

                    var data = JsonConvert.DeserializeObject<DefaultGameData>(json);

                    if (data != null && data.GameOptions != null)
                    {
                        // ⭐ 멀리건 값 로깅 (파일값 → UI값)
                        int mulliganUIValue = ConvertMulliganFileToUI(data.GameOptions.Mulligan_Count);
                        Console.WriteLine($"[GameOptionsManager] 로드 성공");
                        Console.WriteLine($"  [멀리건] 파일값: {data.GameOptions.Mulligan_Count} → UI값: {mulliganUIValue}");
                        Console.WriteLine($"  컨시드: {data.GameOptions.Concede_Distance}");
                        Console.WriteLine($"  잔디상태: {data.GameOptions.Green_Speed}");
                        Console.WriteLine($"  카메라모드: {data.GameOptions.Camera_Mode}");
                        Console.WriteLine($"  스윙모션: {data.GameOptions.SwingMotion}");
                        Console.WriteLine($"  핀위치: {data.GameOptions.HolecupPosition}");
                    }
                    else
                    {
                        Console.WriteLine($"[GameOptionsManager] 경고: 데이터가 null입니다.");
                    }

                    return data;
                }
                else
                {
                    Console.WriteLine($"[GameOptionsManager] 파일이 없습니다. 기본값 생성.");
                    var defaultData = new DefaultGameData();
                    defaultData.Save();
                    return defaultData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameOptionsManager] 로드 실패: {ex.Message}");
                Console.WriteLine($"[GameOptionsManager] 스택트레이스: {ex.StackTrace}");
                return new DefaultGameData();
            }
        }

        /// <summary>
        /// defaultGameData.json 저장
        /// </summary>
        public static void Save(DefaultGameData data)
        {
            try
            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    DefaultGameDataPath);

                // 디렉토리 생성
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, json);

                Console.WriteLine($"[GameOptionsManager] 저장 성공: {filePath}");
                // ⭐ 멀리건 값 로깅 (파일값 그대로)
                Console.WriteLine($"  [멀리건] 파일값: {data.GameOptions.Mulligan_Count}");
                Console.WriteLine($"  컨시드: {data.GameOptions.Concede_Distance}");
                Console.WriteLine($"  잔디상태: {data.GameOptions.Green_Speed}");
                Console.WriteLine($"  카메라모드: {data.GameOptions.Camera_Mode}");
                Console.WriteLine($"  스윙모션: {data.GameOptions.SwingMotion}");
                Console.WriteLine($"  핀위치: {data.GameOptions.HolecupPosition}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameOptionsManager] 저장 실패: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// DefaultGameData 확장 메서드
    /// </summary>
    public static class DefaultGameDataExtensions
    {
        public static void Save(this DefaultGameData data)
        {
            GameOptionsManager.Save(data);
        }
    }
}