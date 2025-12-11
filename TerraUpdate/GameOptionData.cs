using Newtonsoft.Json;
using System;
using System.IO;

namespace Updater
{
    /// <summary>
    /// defaultGameData.json 파일의 GameOptions 구조
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

        [JsonProperty("Mulligan_Count")]
        public int MulliganCount { get; set; } = 1;  // 멀리건 (0=무제한, 1,2,3,5)

        [JsonProperty("Concede_Distance")]
        public int ConcedeDistance { get; set; } = 1;  // 컨시드 (0=없음, 1=1m, 2=1.25m, 3=1.5m, 4=2m)

        [JsonProperty("Green_Speed")]
        public int GreenSpeed { get; set; } = 1;

        [JsonProperty("PracticeBall")]
        public int PracticeBall { get; set; } = 0;

        [JsonProperty("Movie_SaveCount")]
        public int MovieSaveCount { get; set; } = 0;

        [JsonProperty("Camera_Mode")]
        public int CameraMode { get; set; } = 0;

        [JsonProperty("GameType")]
        public int GameType { get; set; } = 1;

        [JsonProperty("SwingMotion")]
        public int SwingMotion { get; set; } = 0;

        // 신규 필드 (게임설정 탭용)
        [JsonProperty("Difficulty")]
        public int Difficulty { get; set; } = 1;  // 난이도 (0=아마추어, 1=세미프로, 2=프로, 3=프로)

        [JsonProperty("PracticeTimeLimit")]
        public int PracticeTimeLimit { get; set; } = 10;  // 연습장 시간제한 (0=무제한, 10,20,30분)

        [JsonProperty("PinPosition")]
        public int PinPosition { get; set; } = 1;  // 핀위치 (0=중앙, 1=앞, 2=뒤, 3=랜덤)
    }

    /// <summary>
    /// GameOptions 관리 클래스
    /// </summary>
    public class GameOptionsManager
    {
        private static readonly string DefaultGameDataPath = "ParkDay\\Saved\\defaultGameData.json";

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
                Console.WriteLine($"[GameOptionsManager] BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");
                Console.WriteLine($"[GameOptionsManager] 전체 경로: {filePath}");
                Console.WriteLine($"[GameOptionsManager] 파일 존재 여부: {File.Exists(filePath)}");

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    Console.WriteLine($"[GameOptionsManager] 파일 내용 길이: {json.Length}");
                    Console.WriteLine($"[GameOptionsManager] 파일 내용:\n{json}");

                    var data = JsonConvert.DeserializeObject<DefaultGameData>(json);

                    if (data != null && data.GameOptions != null)
                    {
                        Console.WriteLine($"[GameOptionsManager] 로드 성공: {filePath}");
                        Console.WriteLine($"  - 멀리건: {data.GameOptions.MulliganCount}");
                        Console.WriteLine($"  - 컨시드: {data.GameOptions.ConcedeDistance}");
                        Console.WriteLine($"  - 난이도: {data.GameOptions.Difficulty}");
                        Console.WriteLine($"  - 연습장 시간제한: {data.GameOptions.PracticeTimeLimit}");
                        Console.WriteLine($"  - 핀위치: {data.GameOptions.PinPosition}");
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