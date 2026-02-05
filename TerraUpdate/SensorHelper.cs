using System;
using System.Runtime.InteropServices;

namespace Updater
{
    /// <summary>
    /// CR2 XCAM 센서 헬퍼 클래스 (공식 CR2 Programming Guide v0.9.31 기반)
    /// 
    /// 문서 출처:
    /// - CR2 Programming Guide (Version 0.9.31, 2022/06/08, Creatz Inc.)
    /// - xcamAdapt_example.cpp (공식 예제)
    /// 
    /// 공식 문서 참고:
    /// - 상태값 정의: 페이지 5-6 <표 4-2> Sensor Status
    /// - 상태 체크: 페이지 23 "2) Sensor Status check"
    /// - CR6CMD_SENSORSTATUS: 페이지 18
    /// </summary>
    public static class SensorHelper
    {
        // ================================================================================
        // CR2 명령어 상수 (공식 문서 <표 4-4> Command Code 값 및 설명, p.6-7)
        // ================================================================================
        public const uint CR2CMD_SENSORSTATUS = 8;      // 센서 상태 조회 (페이지 18)
        public const uint CR2CMD_RGB0_RGB = 12;         // RGB LED 제어
        public const uint CR2CMD_OPERATION_START = 1;   // 센서 동작 시작
        public const uint CR2CMD_OPERATION_STOP = 2;    // 센서 동작 중지
        public const uint CR2CMD_OPERATION_RESTART = 3; // 센서 재시작

        // ================================================================================
        // 반환값 상수
        // ================================================================================
        public const int CR2_OK = 0;  // 성공

        // ================================================================================
        // 센서 상태 상수 (공식 문서 <표 4-2> Sensor Status, p.5-6)
        // ================================================================================
        /// <summary>초기상태</summary>
        public const uint CR2STATUS_NULL = 0;

        /// <summary>Sensor READY 상태 - 센서 준비됨, 샷 가능</summary>
        public const uint CR2STATUS_READY = 1;

        /// <summary>정상 Shot 발생</summary>
        public const uint CR2STATUS_GOODSHOT = 2;

        /// <summary>비정상 Shot이거나 오류 Shot 발생</summary>
        public const uint CR2STATUS_TRIALSHOT = 3;

        /// <summary>Sensor와의 연결이 끊어진 상태 - 실제 오류!</summary>
        public const uint CR2STATUS_DISCONNECT = 4;

        /// <summary>정해진 위치에 ball이 없는 상태</summary>
        public const uint CR2STATUS_NOBALL = 5;

        // ================================================================================
        // RGB 색상 상수 (공식 문서에서 확인)
        // ================================================================================
        public const uint RGB_RED = 0x00FF0000;
        public const uint RGB_GREEN = 0x0000FF00;
        public const uint RGB_BLUE = 0x000000FF;
        public const uint RGB_WHITE = 0x00FFFFFF;
        public const uint RGB_OFF = 0x00000000;

        // RGB 모듈 선택 상수
        public const uint RGB_MODULE_OFF = 0;
        public const uint RGB_MODULE_1 = 1;
        public const uint RGB_MODULE_2 = 2;
        public const uint RGB_MODULE_BOTH = 3;

        // ================================================================================
        // DLL Import (XcamAdapt64.dll - 64bit)
        // ================================================================================
        [DllImport("XcamAdapt64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CR2_command(IntPtr handle, uint cmd, long p0, long p1, long p2, long p3);

        // ================================================================================
        // 센서 초기화 및 제어 메서드
        // ================================================================================

        /// <summary>
        /// 센서 시작 (공식 예제 참고)
        /// CR6CMD_OPERATION_START (1) 명령어 사용
        /// </summary>
        public static bool StartSensor(IntPtr sensorHandle)
        {
            if (sensorHandle == IntPtr.Zero) return false;
            try
            {
                int result = CR2_command(sensorHandle, CR2CMD_OPERATION_START, 0, 0, 0, 0);
                return result == CR2_OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StartSensor 오류] {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 센서 중지 (공식 예제 참고)
        /// CR6CMD_OPERATION_STOP (2) 명령어 사용
        /// </summary>
        public static bool StopSensor(IntPtr sensorHandle)
        {
            if (sensorHandle == IntPtr.Zero) return false;
            try
            {
                int result = CR2_command(sensorHandle, CR2CMD_OPERATION_STOP, 0, 0, 0, 0);
                return result == CR2_OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StopSensor 오류] {ex.Message}");
                return false;
            }
        }

        // ================================================================================
        // 센서 상태 조회 메서드
        // ================================================================================

        /// <summary>
        /// ⭐ 공식 방법: 센서 상태 확인 (CR6CMD_SENSORSTATUS 사용)
        /// 
        /// 공식 문서:
        /// - 페이지 18: CR6CMD_SENSORSTATUS 명령어 설명
        /// - 페이지 23: Sensor Status check 방법
        /// - 예제: xcamAdapt_example.cpp 라인 555-642
        /// 
        /// 동작:
        /// 1. CR6_command(handle, 8, &status, 0, 0, 0) 호출
        /// 2. status 변수에 상태값(0-5) 저장됨
        /// 3. 상태값에 따라 대응
        /// </summary>
        /// <param name="sensorHandle">센서 핸들</param>
        /// <param name="status">출력: 센서 상태값 (0-5)</param>
        /// <param name="statusText">출력: 상태 설명 텍스트</param>
        /// <returns>조회 성공 여부</returns>
        public static bool CheckSensorStatus(IntPtr sensorHandle, out uint status, out string statusText)
        {
            status = CR2STATUS_DISCONNECT;
            statusText = "통신오류";

            if (sensorHandle == IntPtr.Zero)
            {
                statusText = "미초기화";
                return false;
            }

            try
            {
                unsafe
                {
                    // ✅ 공식 방법: 상태값을 저장할 주소를 포인터로 전달
                    uint sensorStatus = 0;
                    uint* statusPtr = &sensorStatus;

                    // ✅ CR6CMD_SENSORSTATUS = 8 (공식 문서 p.18)
                    // p0 = 상태 저장 주소 (포인터)
                    // p1, p2, p3 = 예약 (0)
                    int result = CR2_command(sensorHandle, CR2CMD_SENSORSTATUS, (long)statusPtr, 0, 0, 0);

                    if (result != CR2_OK)
                    {
                        statusText = "통신오류";
                        return false;
                    }

                    status = sensorStatus;

                    // ✅ 상태값 해석 (공식 문서 <표 4-2>, p.5-6)
                    switch (sensorStatus)
                    {
                        case CR2STATUS_NULL:
                            statusText = "초기상태";
                            break;

                        case CR2STATUS_READY:
                            // ✅ 정상 상태: 센서 준비됨, 샷 가능
                            statusText = "준비됨";
                            break;

                        case CR2STATUS_GOODSHOT:
                            // ✅ 정상: 정상 샷 발생
                            statusText = "정상샷";
                            break;

                        case CR2STATUS_TRIALSHOT:
                            // ⚠️ 주의: 비정상 샷, 무시하기 (공식 문서 p.23)
                            statusText = "비정상샷";
                            break;

                        case CR2STATUS_DISCONNECT:
                            // ❌ 실제 오류: 센서 미연결 (공식 문서 p.23)
                            // "센서와의 연결을 점검하세요" 메시지 출력
                            statusText = "연결끊김";
                            Console.WriteLine("[ERROR] 센서와의 연결을 점검하세요 (공식 문서 p.23)");
                            break;

                        case CR2STATUS_NOBALL:
                            // ⚠️ 주의: 볼 없음 (공식 문서 p.23)
                            // "지정된 위치에 ball을 위치시키세요" 메시지 출력
                            statusText = "볼없음";
                            Console.WriteLine("[WARNING] 지정된 위치에 ball을 위치시키세요 (공식 문서 p.23)");
                            break;

                        default:
                            statusText = $"알수없음({sensorStatus})";
                            break;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                statusText = "예외발생";
                Console.WriteLine($"센서 상태 확인 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 센서 연결 테스트 (LED 점등 + 상태 확인)
        /// 
        /// 동작:
        /// 1. LED를 초록색으로 점등 (시각적 연결 확인)
        /// 2. CheckSensorStatus()로 센서 상태 조회
        /// 3. DISCONNECT가 아니면 연결됨으로 판단
        /// </summary>
        public static bool TestSensorConnection(IntPtr sensorHandle)
        {
            if (sensorHandle == IntPtr.Zero)
                return false;

            try
            {
                // 1️⃣ LED 점등 (시각적 연결 확인)
                int ledResult = CR2_command(sensorHandle, CR2CMD_RGB0_RGB, RGB_MODULE_BOTH, RGB_GREEN, 0, 0);
                if (ledResult != CR2_OK)
                    return false;

                // 2️⃣ 실제 상태 확인 (공식 방법 사용)
                if (!CheckSensorStatus(sensorHandle, out uint status, out _))
                    return false;

                // 3️⃣ DISCONNECT만 연결 끊김으로 판단
                // 다른 상태(0,1,2,3,5)는 모두 센서 연결됨
                bool isConnected = status != CR2STATUS_DISCONNECT;
                return isConnected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"센서 연결 테스트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 센서 연결 테스트 (상태 텍스트 포함)
        /// 
        /// 반환:
        /// - bool: 연결 여부 (status != DISCONNECT)
        /// - statusText: 상태 설명 텍스트
        /// </summary>
        public static bool TestSensorConnectionWithStatus(IntPtr sensorHandle, out string statusText)
        {
            statusText = "연결 테스트 중...";

            if (sensorHandle == IntPtr.Zero)
            {
                statusText = "핸들 미초기화";
                return false;
            }

            try
            {
                // 1️⃣ LED 점등
                int ledResult = CR2_command(sensorHandle, CR2CMD_RGB0_RGB, RGB_MODULE_BOTH, RGB_GREEN, 0, 0);
                if (ledResult != CR2_OK)
                {
                    statusText = "LED 명령 실패";
                    return false;
                }

                // 2️⃣ 상태 확인
                if (!CheckSensorStatus(sensorHandle, out uint status, out statusText))
                {
                    statusText = "상태 조회 실패";
                    return false;
                }

                // 3️⃣ 연결 판정
                bool isConnected = status != CR2STATUS_DISCONNECT;
                return isConnected;
            }
            catch (Exception ex)
            {
                statusText = $"예외발생: {ex.Message}";
                Console.WriteLine($"센서 연결 테스트 오류: {ex.Message}");
                return false;
            }
        }

        // ================================================================================
        // LED 제어 메서드
        // ================================================================================

        /// <summary>
        /// 센서 LED 끄기
        /// </summary>
        public static bool TurnOffLED(IntPtr sensorHandle)
        {
            if (sensorHandle == IntPtr.Zero)
                return false;

            try
            {
                int result = CR2_command(sensorHandle, CR2CMD_RGB0_RGB, RGB_MODULE_BOTH, RGB_OFF, 0, 0);
                return result == CR2_OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LED 끄기 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 센서 LED 색상 설정
        /// </summary>
        public static bool SetSensorLED(IntPtr sensorHandle, uint color)
        {
            if (sensorHandle == IntPtr.Zero)
                return false;

            try
            {
                int result = CR2_command(sensorHandle, CR2CMD_RGB0_RGB, RGB_MODULE_BOTH, color, 0, 0);
                return result == CR2_OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LED 제어 오류: {ex.Message}");
                return false;
            }
        }

        // ================================================================================
        // 유틸리티 메서드
        // ================================================================================

        /// <summary>
        /// 센서 상태값을 문자열로 변환
        /// </summary>
        public static string StatusToString(uint status)
        {
            switch (status)
            {
                case CR2STATUS_NULL: return "초기상태";
                case CR2STATUS_READY: return "준비됨";
                case CR2STATUS_GOODSHOT: return "정상샷";
                case CR2STATUS_TRIALSHOT: return "비정상샷";
                case CR2STATUS_DISCONNECT: return "연결끊김";
                case CR2STATUS_NOBALL: return "볼없음";
                default: return $"알수없음({status})";
            }
        }

        /// <summary>
        /// 센서 상태 진단 (연결 여부 판단)
        /// </summary>
        public static bool IsConnected(uint status)
        {
            // ✅ 공식 기준: DISCONNECT만 미연결, 나머지는 연결됨
            return status != CR2STATUS_DISCONNECT;
        }
    }
}