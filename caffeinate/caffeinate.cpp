// caffeinate.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <stdio.h>

#include <windows.h>
#include <strsafe.h>

#include <string>
#include <memory>

void fail(const char* errorMessage)
{
    puts(errorMessage);
    puts("please try again");
    exit(1);
}

std::unique_ptr<wchar_t> reconstructCommandLine(int ndx, int argc, wchar_t* argv[])
{
    std::wstring commandLine{};
    //std::wstr
    for (ndx; ndx < argc; ndx++)
    {
        // TODO: this does not handle " and \ inside args
        commandLine += L"\"";
        commandLine += argv[ndx];
        commandLine += L"\"";
        if ((ndx + 1) != argc)
            commandLine += L" ";
    }
    auto ret = std::unique_ptr<wchar_t>{ new wchar_t[commandLine.length() + 1] };
    StringCchCopyW(ret.get(), commandLine.length(), commandLine.c_str());
    return ret;
}

int wmain(int argc, wchar_t* argv[])
{
    bool caffeinateDisplay = false;
    bool caffeinateSystem = false;
    bool hasTimeout = false;
    DWORD timeoutMs = INFINITE;
    bool expectingTimeoutToFollow = false;

    int ndx = 1;
    for (ndx; ndx < argc; ndx++)
    {
        if (expectingTimeoutToFollow)
        {
            fail("parsing timeouts not yet implemented");
        }
        else if (argv[ndx][0] == '-')
        {
            int charNdx = 1;
            while (argv[ndx][charNdx] != '\0')
            {
                if (expectingTimeoutToFollow)
                {
                    fail("expected timeout value to follow timeout flag");

                }
                switch (argv[ndx][charNdx])
                {
                case 'd':
                    caffeinateDisplay = true;
                    break;
                case 'i':
                    caffeinateSystem = true;
                    break;
                case 't':
                    expectingTimeoutToFollow = true;
                    break;
                default:
                    fail("unknown flag");
                    break;
                }
                charNdx++;
            }
        }
        else
        {
            break;
        }
    }

    if (!caffeinateDisplay && !caffeinateSystem)
    {
        caffeinateSystem = true;
    }

    EXECUTION_STATE esFlags = ES_CONTINUOUS;
    if (caffeinateDisplay)
        esFlags |= ES_DISPLAY_REQUIRED;
    if (caffeinateSystem)
        esFlags |= ES_SYSTEM_REQUIRED;

    SetThreadExecutionState(esFlags);

    if (ndx == argc)
    {
        Sleep(timeoutMs);
        SetThreadExecutionState(ES_CONTINUOUS);
    }
    else
    {
        STARTUPINFO si;
        PROCESS_INFORMATION pi;
        ZeroMemory(&si, sizeof(si));
        si.cb = sizeof(si);
        ZeroMemory(&pi, sizeof(pi));
        auto lpCommandLine = reconstructCommandLine(ndx, argc, argv);
        if (!CreateProcessW(nullptr, lpCommandLine.get(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, &si, &pi))
        {
            fail("failed to create process");
        }

        WaitForSingleObject(pi.hProcess, timeoutMs);

        SetThreadExecutionState(ES_CONTINUOUS);
        
        WaitForSingleObject(pi.hProcess, INFINITE);

        DWORD processExitCode = 0;
        if (!GetExitCodeProcess(pi.hProcess, &processExitCode))
        {
            fail("failed to get exit code");
        }

        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);

        ExitProcess(processExitCode);
    }

    return 0;
}
