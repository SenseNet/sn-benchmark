---
title: "Benchmark details"
source_url: 'https://github.com/SenseNet/sensenet/docs/sn-benchmark'
category: Benchmark
version: v7.0.0
tags: [benchmark, details, parameters, sn7]
---
# Benchmark Details

## The tool
<a name="TheTool"></a>
**SnBenchmark** is a *command line tool* that measures the performance of a sensenet ECM instance and helps you determine real-life capabilities of your site. It requires a running portal, but no modifications are required on the portal side. The tool accesses the site through its *REST API* (or more specifically the [sensenet ECM Client library](http://wiki.sensenet.com/Client_library) built on top of it). As it is written using an *asynchronous* architecture, it does not consume too great amount of resources on the client.

The result of a benchmark execution will be a *csv file* containing request/response times and other collected data that you can use to visualize the behavior of your site under different loads. See an example later in this document.

For debugging purposes a request log will be written after the execution. This file contains information about the last 10000 requests.

## Execution workflow
<a name="ExecutionWorkflow"></a>
This is what the tool does, when you execute it:
1. loads the provided profiles from text files and the given command prompt parameters
2. starts measurement with a short warmup period
3. adds more profiles periodically
4. collects average response times during a period
5. stops adding more profiles when a *predefined response limit* is reached
6. waits while all the remaining requests finish
7. writes the last requests to a log file

As a result, the tool generates a csv file (see below) and provides the last average response times *before the limit has been reached*.

## Profiles
<a name="Profiles"></a>
Benchmark profiles are defined using a simple script language. A profile is a *text file* that consists of lines describing simple actions, like loading a content or performing an [OData action](http://wiki.sensenet.com/OData_REST_API).

We provide a basic set of profiles that we use measuring the core product, but you can create **custom profiles** that better represent your user base.

See an example **Visitor** profile below.

### Usage
<a name="Usage"></a>
There are a couple of *required* and *optional* parameters that you can define the behaviour of the tool.  

_Example:_
```text
SnBenchmark.exe -PROFILE:"Visitor:10+5" -SITE:"http://localhost" -USR:admin -PWD:admin -WARMUP:10 -GROW:10
```
#### Parameters
Parameter order is irrelevant. Parameters have shorter aliases if you prefer them and names are case insensitive.
Acknowledged parameter formats: *-NAME:value* or */NAME:value*

##### PROFILE (alias: P) - required
Comma separated 'name:count+growth|limit' character chains. 
* *name*: profile name (e.g. Visitor)
* *count*: initial number of profiles of that type to start with (required). "Visitor:10" means benchmarking will start with 10 visitors.
* *growth*: number of new profiles added in every period (required, but can be 0). "Visitor:10+1" means that one new visitor profile will be added to the pool periodically (about periods see the *GROW* parameter below).
* *limit*: the maximum number of running instances of this profile (optional). 

```text
Profile1:5+1,Profile2:1+1,Profile3:5+1|50
```

It is recommended to set the number of added profiles to a small number to let the benchmark tool increase the load gradually. Some examples for a complex profile set:
```text
"Visitor:10+2,Aministrator:5+1"
"Visitor:16+4|200,Editor:8+2|20,Aministrator:4+1|10"
```

##### SITE (alias: S) - required
Comma separated url list (e.g.: *'http://mysite1,http://mysite1'*). These urls will be selected and used in a random order and will be supplemented by the relative urls from the current profile.

##### USERNAME (alias: USR) - required
The user's name and domain (e.g. 'Admin' or 'demo\someone). In a future version it will be possible to set a custom user/password for a single request (profile action). Currently these credentials are used for every request.

##### PASSWORD (alias: PWD) - required
Password of the user above.

##### WARMUPTIME (aliases: W, WARMUP) - optional
Warmup time in seconds. Default: 30. During this period the response times are not compared to the limits (see below).

##### GROWINGTIME (aliases: G, GROW, GROWING) - optional
Length of a load period in seconds. Default: 30. Defines a time interval for executing profiles. After it expires, additional  profiles are added to the already running list. The growth rate (number of new profiles) is defined by the *PROFILE* parameter.

##### MAXERRORS (aliases: E, ERRORS) - optional
Maximum allowed error count. Default: 10.

##### OUTPUT (aliases: O, OUT) - optional
Output file for further analysis in csv format. The file is always written to the disk. This parameter controls the output file's name an location. Default location is the *Output* subfolder in the appdomain's base folder (exe location).

Default: *Benchmark-Output_????-??-??_??-??-??.csv* (question marks are replaced by the current date and time value).

The parameter value can be in one of the following formats:
- an absolute path: e.g. "C:\BenchmarkLogs\Monday\Benchmark.csv"
- a relative path: e.g. "..\..\Logs\Benchmark.csv" (base directory is: exelocation\Output)
- a file name: "Benchmark.csv" (directory is: exelocation\Output)
   
The parameter value can contain one *asterisk* (*) as a placeholder character that will be replaced by the current date and time. In the following case the file will be written as *exelocation\Output\Benchmark_2016-09-27_05-38-45.csv*.
```text
-OUT:Benchmark_*.csv
```
##### TESTONLY (aliases: T, TEST) - optional
Plays the initial profile cycle and saves the web responses into separated text files for debugging purposes. Warmup, Growing, Maxerrors parameters are ignored. Using the benchmark tool with this parameter helps to validate the profiles. For example if the profile parameter is **"Profile1:2+2,Profile2:1+1"** and both profiles contain 2 requests, the following structure will be created in the output folder:
```text
Benchmark_2017-05-17_01-46-22   | directory for all trace files
    Profile1                    | directory for Profile1 responses
        Response_P0A0.txt       | first iteration first request
        Response_P0A1.txt       | first iteration second request
        Response_P2A1.txt       | second iteration first request
        Response_P2A1.txt       | second iteration second request
    Profile2                    | directory for Profile2 responses
        Response_P1A0.txt       | first request
        Response_P1A1.txt       | second request
```
Response file name have an unique identifier (e.g. P2A1) that helps identify the source line of the profile. The number after P is the profile id that depends from number of profiles and some random factors but always unique and growing. The number after A is the zero based activity index in the profile. Response files contain the raw web responses.

## Benchmark results

#####  Console output
During running the benchmark tool is writing on the console to make it easier to track the processes in work.  

_Console output example:_
```text
SnBenchmark 1.0.0.0

Initializing profiles.
Ok.
Initializing path sets.
  Getting paths: Downloader.BigFiles ... Ok. Count: 121
Start.
================= MEASUREMENT  Press <x> to exit
INCREASE. 100; 30,834 RPS; 0,00; 0,00
INCREASE. 105; 31,874 RPS; 1,27; 0,87
INCREASE. 110; 32,349 RPS; 0,60; 0,66
Performance max: 35,557; sweetpoint: 33,779
DECREASE. 115; 35,557 RPS; 0,94; 0,91
FINISHED.
BENCHMARK RESULT: Profiles: 115 (Downloader: 92, Uploader50K: 23); RPS: 35,5572; All requests: 26515; Errors: 0; Response times: NORMAL: 0,941; SLOW: 0,907
Finished.
```
The real result of the execution is the written profile count. This is 115 in the mentioned example:
```
BENCHMARK RESULT: Profiles: 115 (Downloader...
```

Meaning of the measurement rows:  
- **INCREASE**: more profile were applied than in the previous iteration
- **DECREASE**: less profile were applied than in the previous iteration
- **Pcount**: currently executing profile count
- **RPS**: average complete request-responses per sec
- **NORMAL, SLOW**: average response times for each speed category

##### Output files
Output files are generated by default in the  ..\BenchmarkTool\bin\Output folder. Every execution generates two files, one for the logging web requests (\*.csv.requests.log) and one for the tracking the measurement data (\*.csv).

A sample request log (truncated):
```
9581	2017-06-09 15:14:49.3073	UPLOAD: fileName:Test.txt target:/.../SysFolder-1, server:http://web02..., actionId:P197A3x
9582	2017-06-09 15:14:49.3386	POST http://web02...('SysFolder2')/Delete?benchmarkId=P234A7x | {"permanent":true}
9583	2017-06-09 15:14:49.4636	GET http://web01.../App4?$select=Name&benchmarkId=P240A4x
9584	2017-06-09 15:14:49.5417	GET http://web02.../App5?$select=Name&benchmarkId=P116A4x
```

Valid benchmark results (\*.csv) can be visualized from the data in the output file.
The output file is in .csv format that can be opened in a spreadsheet software like *Microsoft Excel*.

A sample result file (truncasted):
```
"sep=;"
Benchmark start (UTC):;2017. 11. 13. 7:18:52
Sites:;http://web01.somewhere.com
;http://web02.somewhere.com
Warmup time:;30
Initial profiles:;Uploader50K;1
Growing profiles:;Uploader50K;1
Growing time:;30
Max error count:;10

Pcount;Active;RPS;RPSavg;RPSavg2;RPSdiff;Trigger;NORMAL
1;0;0;0;0;0;0;0,00
1;0;1;0,2;0,04;0,04;0;0,00
1;1;0;0,166666666666667;0,0611111111111111;0,0277777777777778;0;0,00
1;0;1;0,285714285714286;0,0931972789115646;0,0408163265306122;0;0,00
...
BENCHMARK RESULT: Profiles: 19 (Uploader50K: 19); RPS: 7,1405; All requests: 6018; Errors: 0; Response times: NORMAL: 3,791
Max performance (RPS);7,14045
Sweet point (RPS);6,7834275
```

##### Structure of the output file
The first few lines of the file contain the execution values (e.g. start time) for logging purposes.

Below that there is a running log table that can be visualized as a graph. 

Follow these steps to make a graph out of benchmark result data in MS Excel:

1. Delete the benchmark info at the top, so that the table header becomes the first row.
2. Select all relevant columns: click (and drag) the appropriate column headers (usually the first five columns are sufficient: A-E).
3. Click the "INSERT" tab in the ribbon, click the "Insert line chart" and select the first 2-D line chart.
4. Resize the chart if necessary.

![alt text](images/benchmark-details-1.png "Logo Title")

Focused to open (active) requests, finished requests (RPS) and its average (RPSAvg2):
![alt text](images/benchmark-details-2.png "Logo Title")

