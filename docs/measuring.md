---
title: "Benchmark measuring"
source_url: 'https://github.com/SenseNet/sn-benchmark/tree/master/docs/measuring.md'
category: Benchmark
version: v7.0.0
tags: [benchmark, measuring, workflow, sn7]
---

# Benchmark measuring
This page discusses the sensenet Benchmark Tool measuring process. Further information about measuring profiles can be found on the following page:
 - [Profiles](/docs/profile-definition-language.md)

## Steps before measuring
  - Make sure you have a clean database and prepared for measuring.
  - Check that LuceneIndex is in sync on all sites.
  - On the benchmark servers all sites (except the measured one) must be stopped.
  - Check benchmark output directory: `..\BenchmarkTool\bin\Output`
  - Start the measuring app pools and sites
## Measuring
  Execute the benchmark tool:  
	```
	..BenchmarkTool\bin\SnBenchmark.exe -GROW:30 -ERRORS:10 -WARMUP:30 -SITE:"http://somehost.com,http://site01.com" -USR:admin -PWD:admin
	```  

Further explanation on parameters is in the [benchmark details documentation](/docs/benchmark-details.md).

If you would like reliable results, you should repeat the process 2-5 times. Between the iterations it is recommended to wait for your web servers to free the allocated resources.

## Steps after measuring
The measuring makes .csv files from the measured data, but if there were errors .error files are also generated.

You should take the result into account only if there were no errors during measuring. Thankfully the value list is in .csv files, so you can use any spreadsheet handling software to evaluate the results easily.
