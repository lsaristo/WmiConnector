// This file provides basic support to write to a log file located somwhere
// on disk. 
#include<Windows.h>
#include<tchar.h>

int __cdecl _tmain()
{
    TCHAR filename[] = _T("\\\\backups\\computerimagingprimary\\resources\\ImageCreation.log");
    TCHAR name[32767];
    DWORD nameSize = 32767;      
    SYSTEMTIME time;
    TCHAR data[sizeof(TCHAR) * 1024];
    TCHAR result[] = _T("Result: SUCCESS\r\n");
    GetSystemTime(&time);                            
    GetComputerName(&(*name), &nameSize);

    _stprintf_s(
        data, 
        _T("%2.2i-%2.2i-%4.4i, %2.2i-%2.2i: "), 
        time.wMonth, 
        time.wDay, 
        time.wYear, 
        time.wHour, 
        time.wMinute
    );
    _tcscat_s(data, _T("Hostname: "));
    _tcscat_s(data, &(*name));
    _tcscat_s(data, _T(", "));
    _tcscat_s(data, result);
    
    HANDLE file = CreateFile(
        filename,
        FILE_APPEND_DATA,
        0,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL
    );

    if (file == INVALID_HANDLE_VALUE) { return; }
    WriteFile(file, data, _tcslen(data)*sizeof(TCHAR), NULL, NULL);
    CloseHandle(file);
}
