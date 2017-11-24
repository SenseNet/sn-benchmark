# sensenet ECM Benchmark
[![Join the chat at https://gitter.im/SenseNet/sn-benchmark](https://badges.gitter.im/SenseNet/sn-benchmark.svg)](https://gitter.im/SenseNet/sn-benchmark?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[sensenet ECM](http://sensenet.com), an enterprise content management platform that is capable of handling many users and a huge number of content in a single Content Repository, constantly evolves. This requires a stable **benchmarking method** that lets us measure the impact of our decisions along the way. It may also help measuring performance on specific implementations, not just the core platform.

![alt text](docs\images\1-evaluation.png "performance ladder")

## Table of contents
1. [How it works](#HowItWorks)
7. [Profile example: the Visitor](#ProfileExampleTheVisitor)
9. [Resources](#Resources)
10. [Hardware](#Hardware)

## How it works
<a name="HowItWorks"></a>

sensenet ECM benchmarking is built around **Profiles**. A profile represents a typical user (e.g. Visitor, Editor) whose activity can be described by a **set of simple actions**. At the first phase the benchmark tool starts to put load on the site with a configured number of profiles, then it adds more and more gradually. The tool stops when the portal starts responding slower than a predefined treshold.

The idea is that after a few iterations you will be able to determine the **optimal set of profiles** (e.g. 40 Visitors, 5 Editors and 2 Administrators) that may use the portal concurrently without significant performance degradation.

### Profile example: Visitor
=======

<a name="ProfileExampleTheVisitor"></a>
A Visitor usually performs actions similar to these:
1. visit the main page
2. perform a search
3. paging on the search page
4. visit one of the results

This is how it looks like translated to the script language of the SnBenchmark tool. Note the delays between the requests (defined in seconds) that ensure the profile simulates a real life user correctly.

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

For a more complex example please check the Editor profile provided by us in the source directory. If you want to create a custom profile, put those text files in the *Profiles* folder next to the tool. See the language details in the [Profile Definition Language Guide](/docs/profile-definition-language.md).

## Resources
<a name="Resources"></a>
The SnBenchmark tool itself does not consume too much resources. It is written as a completely asynchron tool, so you can start it on an average machine.

You should pay attention to the generated log files though: after a few iterations the csv or error files but especially request logs and test directories may consume a significant amount of disk space.

## Hardware
<a name="Hardware"></a>
The hardware your site runs on is important from the measurement's point of view. It does not make sense to compare two performance measurements made on a different hardware - unless you want to measure the performance of the hardware that usually is not the case.
