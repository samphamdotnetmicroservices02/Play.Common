using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MassTransit;
using OpenTelemetry.Trace;

namespace Play.Common.MassTransit;

// After configuring Jaeger
/*
* When you visualize the tracing information, it is great that we are able to see the times that each different call took
* across all the invocations. But one more thing that we can show there is also the fact that there may have been some
* error when one of the operations happened. In particular, when we try to consume one of the messages from mass transit,
* if there was some error we should be able to report that into that visualization in the Jaeger portal. To do that, we're
* going to do just to implement what we call a consumer observer, it's a facility of mass transit that will allow us to 
* do something when any of our consumers have an error and that's something is going to be that we're going to tag that
* activity, that tracing activity, so that it can be utilized later on in ther Jaeger portal.
*/
public class ConsumeObserver : IConsumeObserver
{
    /*
    * this method is gonna be invoked if any of our consumers have drawn an exception for some reason. And that's the point
    * where we want to tag the trace activity with some information.
    */
    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
    {
        Activity.Current.SetStatus(Status.Error.WithDescription(exception.Message));

        return Task.CompletedTask;
    }

    public Task PostConsume<T>(ConsumeContext<T> context) where T : class
    {
        return Task.CompletedTask;
    }

    public Task PreConsume<T>(ConsumeContext<T> context) where T : class
    {
        return Task.CompletedTask;
    }
}