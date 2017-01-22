@ECHO OFF
pushd .

SET XMLLOGFILENAME="nunit.results.xml"

REM ===========================
REM Find toolpath for OpenCover
REM ===========================
ECHO ********** Searching %USERPROFILE%\.nuget\packages\OpenCover for OpenCover executable...
FOR /R %USERPROFILE%\.nuget\packages\OpenCover %%f IN (OpenCover.Console.exe) DO (
  IF EXIST %%f (
    SET OPENCOVER=%%f
    GOTO FOUNDOPENCOVER
  )
)
IF '%OPENCOVER%'=='' GOTO FAILEDOPENCOVER
:FOUNDOPENCOVER
ECHO ********** Found OpenCover at %OPENCOVER%

REM =================================
REM Find toolpath for ReportGenerator
REM =================================
ECHO ********** Searching %USERPROFILE%\.nuget\packages\ReportGenerator for ReportGenerator executable...
FOR /R %USERPROFILE%\.nuget\packages\ReportGenerator %%f IN (ReportGenerator.exe) DO (
  IF EXIST %%f (
    SET REPORTGENERATOR=%%f
    GOTO FOUNDREPORTGENERATOR
  )
)
IF '%REPORTGENERATOR%'=='' GOTO FAILEDREPORTGENERATOR
:FOUNDREPORTGENERATOR
ECHO ********** Found ReportGenerator at %OPENCOVER%

REM ===========================================================
REM Run the code coverage program and produce a coverage report
REM ===========================================================
cd tests\Searchlight.Tests
ECHO ********** Creating the coverage folder...
mkdir coverage
ECHO ********** Clearing out previous test run data...
del coverage\*.* /f/s/q
ECHO ********** Run all tests and collect coverage data...
%OPENCOVER% -register:user -target:"%1dotnet.exe" -targetargs:"test -c Debug" -output:coverage\coverage.xml -oldStyle
ECHO ********** Build integration test code coverage reports...
%REPORTGENERATOR% "-reports:coverage\coverage.xml" "-targetdir:coverage"
ECHO ********** Completed!
start coverage\index.htm
GOTO DONE

REM =================================================
REM Notify the user that OpenCover could not be found
REM =================================================
:FAILEDOPENCOVER
ECHO ********** Failed to find OpenCover.  Cannot continue.
GOTO DONE

REM =======================================================
REM Notify the user that ReportGenerator could not be found
REM =======================================================
:FAILEDREPORTGENERATOR
ECHO ********** Failed to find Code Coverage Report Generator.  Cannot continue.
GOTO DONE

REM ========
REM Clean up
REM ========
:DONE
popd 

