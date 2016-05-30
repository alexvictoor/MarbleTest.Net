[![Build status](https://ci.appveyor.com/api/projects/status/0964n7sqp77j4v5u?svg=true)](https://ci.appveyor.com/project/alexvictoor/marbletest-net)

# MarbleTest.Net

MarbleTest.Net is a tiny library that allows to write tests for codes based on Rx.Net using marble diagrams in ASCII form.  
This is a C# port of the [marble test features](https://github.com/ReactiveX/rxjs/blob/master/doc/writing-marble-tests.md) of amazing RxJS v5.

## Quickstart

To get the lib just use nuget as below:
```
PM> Install-Package Marble.Test -Pre
```

## Usage

The API sticks to the RxJS one. The main difference is that instead of using a good old TestScheduler, you will need a **MarbleScheduler**: 
```
var scheduler = new MarbleScheduler();
``` 
This scheduler can then be used to configure source observables:
```
var sourceEvents = _scheduler.CreateColdObservable("a-b-c-|");
```
Then you can use the **MarbleScheduler.ExpectObservable()** to verify that everything wwent as expected during the test. 
Below a really simple all-in-one example: 
```
var sourceEvents = _scheduler.CreateColdObservable("a-b-c-|");      // create the input events
var upperEvents = sourceEvents.Select(s => s.ToUpper());            // transform the events - this is what we often call the SUT ;)
_scheduler.ExpectObservable(upperEvents).ToBe("A-B-C-|");           // check that the output events have the timing and values as expected
_scheduler.Flush();                                                 // let the virtual clock goes... otherwise nothing happens
```

## Marble ASCII syntax
Each ASCII character represents what happens during 10ms.  
'-' means that nothing happens  
Any letter means that an event occurs  
'|' means the stream end successfuly
'#' means an error occurs

So "a-b-|" means:

- At 0, an event 'a' occurs
- Nothing till 20 where an event 'b' occues
- Then the stream ends at 40

## Events types
TODO
## Features
TODO