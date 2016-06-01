[![Build status](https://ci.appveyor.com/api/projects/status/0964n7sqp77j4v5u?svg=true)](https://ci.appveyor.com/project/alexvictoor/marbletest-net)

# MarbleTest.Net

MarbleTest.Net is a tiny library that allows to write tests for codes based on Rx.Net using marble diagrams in ASCII form.  
This is a C# port of the [marble test features](https://github.com/ReactiveX/rxjs/blob/master/doc/writing-marble-tests.md) of amazing RxJS v5.  
The purpose of the library is to help you write as concise an readable tests when dealing with Rx code. 

## Quickstart

To get the lib just use nuget as below:
```
PM> Install-Package MarbleTest.Net
```

## Usage

The API sticks to the RxJS one. The main difference is that instead of using a good old TestScheduler, you will need a **MarbleScheduler**: 
```
var scheduler = new MarbleScheduler();
``` 
This scheduler can then be used to configure source observables:
```
var sourceEvents = scheduler.CreateColdObservable("a-b-c-|");
```
Then you can use the **MarbleScheduler.ExpectObservable()** to verify that everything went as expected during the test. 
Below a really simple all-in-one example: 
```
var scheduler = new MarbleScheduler();
var sourceEvents = scheduler.CreateColdObservable("a-b-c-|"); // create an IObservable<string> emiting 3 "next" events
var upperEvents = sourceEvents.Select(s => s.ToUpper());      // transform the events - this is what we often call the SUT ;)
scheduler.ExpectObservable(upperEvents).ToBe("A-B-C-|");      // check that the output events have the timing and values as expected
scheduler.Flush();                                            // let the virtual clock goes... otherwise nothing happens
```
**Important:** as shown above, do not forget to **Flush** the scheduler at the end of your test case, otherwise no event will be emitted. 

In the above examples, event values are not specified and string streams are produced (i.e. IObservable<string>).  
As with the RxJS api, you can use a parameter object containing event values:
```
IObservable<int> events = scheduler.CreateHotObservable<int>("a-b-c-|", new { a = 1, b = 2, c = 3});
```


## Marble ASCII syntax

The syntax remains exactly the same as the one of RxJS.   
Each ASCII character represents what happens during a time interval, by default 10 ticks.  
**'-'** means that nothing happens  
**'a'** or any letter means that an event occurs  
**'|'** means the stream end successfully  
**'#'** means an error occurs

So "a-b-|" means:

- At 0, an event 'a' occurs
- Nothing till 20 where an event 'b' occurs
- Then the stream ends at 40

If some events occurs simultanously, you can group them using paranthesis.  
So "--(abc)--" means events a, b and c occur at time 20.  

For an exhaustive description of the syntax you can checkout 
the [official RxJS documentation](https://github.com/ReactiveX/rxjs/blob/master/doc/writing-marble-tests.md)

## Advanced features

For a complete listof supported features you can checkout 
the [tests of the MarbleScheduler class](https://github.com/alexvictoor/MarbleTest.Net/blob/master/MarbleTest.Net.Test/MarbleSchedulerTest.cs).