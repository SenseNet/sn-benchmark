# Sense/Net Benchmark
[![Join the chat at https://gitter.im/SenseNet/sn-benchmark](https://badges.gitter.im/SenseNet/sn-benchmark.svg)](https://gitter.im/SenseNet/sn-benchmark?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[Sense/Net ECM](http://sensenet.com), an enterprise content management platform that is capable of handling many users and a huge number of content in a single Content Repository, constantly evolves. This requires a stable **benchmarking method** that lets us measure the impact of our decisions along the way. It may also help measuring performance on specific implementations, not just the core platform.

![Benchmark diagram 1](http://wiki.sensenet.com/images/6/66/Becnhmark-01.png "Benchmark diagram 1")

## Table of contents
1. [How it works](#HowItWorks)
2. [The tool](#TheTool)
3. [Execution workflow](#ExecutionWorkflow)
4. [Profiles](#Profiles)
5. [Usage](#Usage)
6. [Benchmark results](#BenchmarkResults)
7. [Profile example: the Visitor](#ProfileExampleTheVisitor)
8. [Benchmark Profile Definition Language](#BenchmarkProfileDefinitionLanguage)
9. [Resources](#Resources)
10. [Hardware](#Hardware)

## How it works
<a name="HowItWorks"></a>
Sense/Net ECM benchmarking is built around **Profiles**. A profile represents a typical user (e.g. Visitor, Editor) that can be described by a **set of simple actions**. The benchmark tool will start to "attack" the site with a configured number of profiles (e.g. 20 Visitors) then it adds more and more visitors gradually. The tool stops when the portal starts responding slower than a predefined treshold.

The idea is that after a few iterations you will be able to determine the **optimal set of profiles** (e.g. 40 Visitors, 5 Editors and 2 Administrators) that may use the portal concurrently without significant performance degradation.

## The tool
<a name="TheTool"></a>
**SnBenchmark** is a *command line tool* that measures the performance of a Sense/Net ECM instance and helps you determine real-life capabilities of your site. It requires a running portal, but no modifications are required on the portal side. The tool accesses the site through its *REST API* (or to be precise the [Sense/Net Client library](http://wiki.sensenet.com/Client_library) built on top of it). As it is written using an *asynchronous* architecture, it does not consume too much resources on the client you are executing it on.

The result of a benchmark execution will be a *csv file* containing request/response times and other collected data that you can use to visualize the behavior of your site under different load. See an example later in this document.

## Execution workflow
<a name="ExecutionWorkflow"></a>
This is what the tool does, when you execute it:
1. loads the provided profiles from text files
2. starts measurement with a short warmup period
3. adds more profiles periodically
4. collects average response times during a period
5. stops adding more profiles when a *predefined hypotetical limit* is reached
6. waits while all the remaining requests finish

As a result, the tool will generate a csv file (see below) and it will provide the last average response times *before the limit has been reached*.

## Profiles
<a name="Profiles"></a>
Benchmark profiles are defined using a simple script language. A profile is a *text file* that consists of lines describing simple actions, like loading a content or performing an [OData action](http://wiki.sensenet.com/OData_REST_API).

We provide a basic set of profiles that we use for measuring the core product, but you can create **custom profiles** that better represent your user base.

See an example **Visitor** profile below.

## Usage
<a name="Usage"></a>
There are a couple of *required* and *optional* parameters by which you can define the behavior of the tool.
```text
SnBenchmark.exe -PROFILE:"Visitor:10+5" -SITE:"http://localhost" -USR:admin -PWD:admin -WARMUP:10 -GROW:10 -LIMIT:"Normal:4.0;Slow:8.0"
```
Parameter order is irrelevant. Parameters have shorter aliases if you prefer them and names are case insensitive.
Recommended parameter format: *-NAME:value* or */NAME:value*

##### PROFILE (alias: P) - required
Comma separated 'name:count+growth|limit' structures. 
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
Warmup time in seconds. Default: 60. During this period the response times are not compared to the limits (see below).

##### GROWINGTIME (aliases: G, GROW, GROWING) - optional
Length of a load period in seconds. Default: 30. Defines a time interval for executing profiles. After it expires, additional  profiles are added to the already running list. The growth rate (number of new profiles) is defined by the *PROFILE* parameter.

##### LIMIT (alias: L) - optional
Average *response time limits* per speed category in seconds (e.g.: NORMAL:4;FAST:2;SLOW:6.3). The speed category default is 10. Profiles can assign a speed category to every request definition. Undefined speed is the default: NORMAL.

During benchmarking the average response times are calculated by speed category. If the average response time exceeds the category limit, the measuring will be terminated. The benchmark result is the average value *before* any limit has been reached. It is strongly recommended to define only a few categories (3-5 types max).

##### MAXERRORS (aliases: E, ERRORS) - optional
Maximum allowed error count. Default: 10.

##### VERBOSE (alias: V) - optional
If set, a detailed benchmark measuring progress is written to the console. This parameter has no value.

##### OUTPUT (aliases: O, OUT) - optional
Output file for further analysis in csv format. The file is always written to the disk. This parameter controls the output file's name an location. Default location is the *Output* subirectory in the appdomain's base directory (exe location).

Default: *Benchmark-Output_????-??-??_??-??-??.csv* (question marks are replaced by the current date and time).

The parameter value can be in one of the following formats:
- an absolute path: e.g. "C:\BenchmarkLogs\Monday\Benchmark.csv"
- a relative path: e.g. "..\..\Logs\Benchmark.csv" (base directory is: exelocation\Output)
- a file name: "Benchmark.csv" (directory is: exelocation\Output)
   
The parameter value can contain one *asterisk* (*) as a placeholder character that will be replaced by the current date and time. In the following case the file will be written as *exelocation\Output\Benchmark_2016-09-27_05-38-45.csv*.
```text
-OUT:Benchmark_*.csv
```

## Benchmark results
<a name="BenchmarkResults"></a>
#####  Console output
Default:
```text
Pcount  Active  NORMAL  SLOW
                2,00    5,00
10      0       0,00    0,00 ---- WARMUP  -----
15      2       0,00    0,00 ---- GROWING -------------
```

Verbose:
```text
Pcount  Active  Req/sec NORMAL  SLOW    Lnormal Lslow
---- WARMUP
10      0       0       0,00    0,00    2,00    5,00
10      3       4       0,00    0,00    2,00    5,00
---- GROWING
15      2       1       0,00    0,00    2,00    5,00
15      4       6       0,00    0,00    2,00    5,00
15      7       7       0,00    0,00    2,00    5,00
```

##### Headers
- **Pcount**: Currently executing profile count.
- **Active**: average active requests (request sent but response has not arrived yet).
- **Req/sec**: average complete request-responses per sec.
- **NORMAL, SLOW**: average response times for each speed category.
- **Lnormal, Lslow**: Speed category limits (control values for drawing charts).

##### Output file
The benchmark result cannot be an exact value because the limits are set by us and they are just theoretical values that ensure that the tool terminates. Valid benchmark results can be visualized from the data in the output file.

The output file is the CSV mutation of the verbose console that can be opened in *Microsoft Excel*. The first few lines of the file contain the execution values (e.g. start time) for logging purposes.

Below that there is a running log table that can be visualized as a graph. Follow these steps to do that.
1. Delete the benchmark info at the top, so that the table header becomes the first row.
2. Select all relevant columns: click the "D" column header and drag right to the last column (the first 3 columns are not needed for the chart).
3. Click the "INSERT" tab in the ribbon, click the "Insert line chart" and select the first 2-D line chart.
4. Resize the chart if necessary.

On the chart the horizontal lines are the speed category limits. You can see the average response times for each speed category. The first section (on the left with many zeroes) is whe warmup. 

(On the right side there is a break in all lines that is caused by an extra line in the table. This line is the benchmark result that was written when one of the average values reached the defined limit. You can delete the line and the break disappears.)

As you can see on the diagram above, there was a point during the benchmark when average request times started to grow significantly. That point is way before reaching the defined speed limit (the flat line above), but that is the purpose of this benchmark: we are looking for the optimal set of profiles that can be served by the portal without performance loss. So in the example above the maximum number of profiles (in this hardware environment) is determined by the red arrows. 

## Profile example: the Visitor
<a name="ProfileExampleTheVisitor"></a>
A Visitor usually performs actions similar to these:
1. visit the main page
2. perform a search
3. paging on the search page
4. visit one of the results

This is how it looks like translated to the script language of the SnBenchmark tool. Note the delays between the requests (defined in seconds): the ensure that the profile simulates a real life user correctly.

```text
; get main page
REQ: /
WAIT: 2000

; quick search by a simple word: "demo" (through the REST API
REQ: GET /OData.svc/Root/Sites/Default_Site?$top=10&query=demo&$inlinecount=allpages&metadata=no

; get the search result html page once to simulate real usage
REQ: /features/Search?text=demo
WAIT: 2000

; get 2nd page of the search result
REQ: /OData.svc/Root/Sites/Default_Site?$top=10&$skip=10&query=demo&$inlinecount=allpages&metadata=no
WAIT: 2000

; get 3rd page of the search result
REQ: /OData.svc/Root/Sites/Default_Site?$top=10&$skip=20&query=demo&$inlinecount=allpages&metadata=no
WAIT: 2000

; get 4th page of the search result
REQ: /OData.svc/Root/Sites/Default_Site?$top=10&$skip=30&query=demo&$inlinecount=allpages&metadata=no
WAIT: 2000

; browse one of the result content
REQ: /OData.svc/infos/features('kpiworkspacesinfo')
WAIT: 8000
```

For a more complex example please check the Editor profile provided by us in the source directory. If you want to create a custom profile, put those text files in the *Profiles* folder next to the tool. See the language details in the following section.

## Benchmark Profile Definition Language
<a name="BenchmarkProfileDefinitionLanguage"></a>
The Profile Definition is a text file that describes an action sequence. Possible actions: sending a web request, memorizing data from the response as a variable, waiting for a couple of seconds.
 
At the beginnig of the line there is a *control character or word* that defines the purpose of that line. Empty lines and lines starting with an unrecognized control word will be skipped.

### Comment
If the line starts with a semicolon character (;) the line will be skipped.
```text
; PROFILE DESCRIPTION: Small content editor task
```
There is only line comment, it is not possible to write an inline comment.

### WAIT
The profile execution will be suspended for a time. Parameter value is defined in milliseconds.
```text
WAIT: 2000
```
This causes a 2 seconds async pause.

### REQ
Describes a web request. Two parameters are allowed: *http verb* and *url**
- HTTP verb: GET, POST, DELETE and so on. If the verb is missing, the *default is GET*.
- URL: absolute url without the protocol and domain. Must start with a slash (/).
```text
; Default main page GET request
REQ: /
; Subpage html request
REQ: GET /workspaces
; OData request
REQ: DELETE /odata.svc/content(1234)
```

There are a couple of extensions to the request line above. They can be written as separate lines after a request line. The following keywords can be used: *DATA, SPEED, VAR*

#### DATA
There are many OData requests that require some data in the request stream. This is how you define a post data.
```text
; Create a new memo
REQ: POST /OData.svc/workspaces/Project/madridprojectworkspace/Memos
DATA: models=[{"__ContentType":"Memo","Description":"asdf qwer"}]
```

#### SPEED
Defines the *speed category* of the current request. Optional, default is "NORMAL". Category name is case insensitive. Categories are defined when you execute the benchmark tool (see parameters above). Requests that are put into a certain category will be taken into account when we compare the average response times to category limits.
```text
SPEED: Slow
```

#### VAR
There is a simple way to memorize and reuse data from an OData response: you can define a variable that will hold a value extracted from the response object. This is useful when you are working with dynamic content items and values that you do not know at the time when you create the script.

Response object reference id *"@Response"*.

Variable names must start with the *'@'* character. Only a property path is allowed on the right side, expressions are forbidden.
```text
VAR: @Name = @Response.d.Name
```

### Templating: using variables
In request urls or data it is possible to mark places for substitution with variables defined earlier.

Placeholder format: *<< variablename >>*.

```text
; Create a new memo and memorize its name (generated by the server)
REQ: POST /OData.svc/workspaces/Project/madridprojectworkspace/Memos
DATA: models=[{"__ContentType":"Memo","Description":"asdf qwer"}]
VAR: @Name = @Response.d.Name

; 5 seconds pause
WAIT: 5000

; Modify a field of the created content
REQ: PATCH /OData.svc/workspaces/Project/madridprojectworkspace/Memos('<<@Name>>')
DATA: models=[{"Description":"description of <<@Name>>"}]
```

## Resources
<a name="Resources"></a>
The SnBenchmark tool itself does not consume too much resources. It is written as a completely asynchronous tool, you can start it on an average machine.

You should pay attention to the generated log files though: after a few iterations the csv or error files may consume a significant amout of disk space.

## Hardware
<a name="Hardware"></a>
The hardware your site runs on is important from the measurement's point of view. It does not make sense to compare two performance measurements made on a different hardware - unless you want to measure the performance of the hardware which is usually not the case.
