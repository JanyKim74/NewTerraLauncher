using System;
using System.Runtime.InteropServices;

namespace Updater
{
    /// <summary>
    /// CR2 XCAM 센서 헬퍼 클래스
    /// RGB LED 테스트를 통한 간단한 센서 연결 확인 + 정확한 상태 조회
    /// </summary>
    public static class SensorHelper
    {
        // CR2 명령어 상수
        public const uint CR2CMD_SENSORSTATUS = 8;
        public const uint CR2CMD_RGB0_RGB = 12;
        public const int CR2_OK = 0;

        // 센서 상태 상수 (CR2 문서 기준)
        public const uint CR2STATUS_NULL = 0;        // 초기상태
        public const uint CR2STATUS_READY = 1;       // 센서 준비됨
        public const uint CR2STATUS_GOODSHOT = 2;    // 정상 샷 발생
        public const uint CR2STATUS_TRIALSHOT = 3;   // 비정상 샷 발생
        public const uint CR2STATUS_DISCONNECT = 4;  // 센서 연결 끊김
        public const uint CR2STATUS_NOBALL = 5;      // 볼 없음

        // RGB 색상 상수 (0x00RRGGBB 형식)
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


        public const uint CR2CMD_OPERATION_START = 0x01;  // PDF 7쪽: 실제 값은 헤더에서 확인, 예시로 1 (샘플 확인 필요)
        public const uint CR2CMD_OPERATION_STOP = 0x02;   // 실제 값 확인 (PDF 7쪽 목록)



        // 추가: 센서 동작 시작 (콜백 없음)
        // 공용: 센서 시작
        public static bool StartSensor(IntPtr sensorHandle)
        {
            if (sensorHandle == IntPtr.Zero) return false;
            try
            {
                int result = CR2_command(sensorHandle, 1, 0, 0, 0, 0); // CR2CMD_OPERATION_START = 1
                return result == CR2_OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StartSensor 오류] {ex.Message}");
                return false;
            }
        }

        // 공용: 센서 중지
        public static bool StopSensor(IntPtr sensorHandle)
        {
            if (sensorHandle == IntPtr.Zero) return false;
            try
            {
                int result = CR2_command(sensorHandle, 2, 0, 0, 0, 0); // CR2CMD_OPERATION_STOP = 2
                return result == CR2_OK;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StopSensor 오류] {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// 센서 상태 확인 (CR2CMD_SENSORSTATUS 사용)
        /// </summary>
        /// <param name="sensorHandle">센서 핸들</param>
        /// <param name="status">출력: 센서 상태값</param>
        /// <param name="statusText">출력: 상태 텍스트</param>
        /// <returns>통신 성공 여부 (실제 연결 여부 아님)</returns>
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
                    uint sensorStatus = 0;
                    uint* statusPtr = &sensorStatus;

                    int result = CR2_command(sensorHandle, CR2CMD_SENSORSTATUS, (long)statusPtr, 0, 0, 0);

                    if (result != CR2_OK)
                    {
                        statusText = "통신오류";
                        return false;
                    }

                    status = sensorStatus;

                    switch (sensorStatus)
                    {
                        case CR2STATUS_READY:
                            statusText = "준비됨";
                            break;
                        case CR2STATUS_NOBALL:
                            statusText = "볼 없음";
                            break;
                        case CR2STATUS_NULL:
                            statusText = "초기상태";
                            break;
                        case CR2STATUS_GOODSHOT:
                            statusText = "정상샷";
                            break;
                        case CR2STATUS_TRIALSHOT:
                            statusText = "비정상샷";
                            break;
                        case CR2STATUS_DISCONNECT:
                            statusText = "연결끊김";
                            break;
                        default:
                            statusText = "알 수 없음";
                            break;
                    }

                    return true; // 통신 성공
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
        /// 센서 연결 테스트 (녹색 LED 점등 + 상태 확인)
        /// </summary>
        /// <param name="sensorHandle">센서 핸들</param>
        /// <returns>센서 연결 여부 (DISCONNECT 아님)</returns>
        public static bool TestSensorConnection(IntPtr sensorHandle)
        {
            if (sensorHandle == IntPtr.Zero)
                return false;

            try
            {
                // 1. LED 점등 (시각적 연결 확인)
                int ledResult = CR2_command(sensorHandle, CR2CMD_RGB0_RGB, RGB_MODULE_BOTH, RGB_GREEN, 0, 0);
                if (ledResult != CR2_OK)
                    return false;

                // 2. 실제 상태 확인
                if (!CheckSensorStatus(sensorHandle, out uint status, out _))
                    return false;

                // DISCONNECT만 연결 끊김으로 판단
                return status != CR2STATUS_DISCONNECT;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"센서 연결 테스트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 센서 LED 끄기
        /// </summary>
        /// <param name="sensorHandle">센서 핸들</param>
        /// <returns>성공 여부</returns>
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

        // DLL Import (CR2_command만 필요)
        [DllImport("XcamAdapt64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CR2_command(IntPtr handle, uint cmd, long p0, long p1, long p2, long p3);




    }
}