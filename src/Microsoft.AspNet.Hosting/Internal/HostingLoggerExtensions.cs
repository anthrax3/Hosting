﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Hosting.Internal
{
    internal static class HostingLoggerExtensions
    {
        public static IDisposable RequestScope(this ILogger logger, HttpContext httpContext)
        {
            return logger.BeginScopeImpl(new HostingLogScope(httpContext));
        }

        public static void RequestStarting(this ILogger logger, HttpContext httpContext)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: 1,
                    state: new HostingRequestStarting(httpContext),
                    exception: null,
                    formatter: HostingRequestStarting.Callback);
            }
        }

        public static void RequestFinished(this ILogger logger, HttpContext httpContext)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(
                    logLevel: LogLevel.Information,
                    eventId: 2,
                    state: new HostingRequestFinished(httpContext),
                    exception: null,
                    formatter: HostingRequestFinished.Callback);
            }
        }

        private class HostingLogScope : ILogValues
        {
            private readonly HttpContext _httpContext;

            private string _cachedToString;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;

            public HostingLogScope(HttpContext httpContext)
            {
                _httpContext = httpContext;
            }

            public override string ToString() => _cachedToString ?? Interlocked.CompareExchange(
                ref _cachedToString,
                $"RequestId:{_httpContext.TraceIdentifier} RequestPath:{_httpContext.Request.Path}",
                null);

            public IEnumerable<KeyValuePair<string, object>> GetValues() => _cachedGetValues ?? Interlocked.CompareExchange(
                ref _cachedGetValues,
                new[]
                {
                    new KeyValuePair<string, object>("RequestId", _httpContext.TraceIdentifier),
                    new KeyValuePair<string, object>("RequestPath", _httpContext.Request.Path.ToString()),
                },
                null);
        }

        private class HostingRequestStarting : ILogValues
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestStarting)state).ToString();

            private readonly HttpContext _httpContext;

            private string _cachedToString;
            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;

            public HostingRequestStarting(HttpContext httpContext)
            {
                _httpContext = httpContext;
            }

            public override string ToString() => _cachedToString ?? Interlocked.CompareExchange(
                ref _cachedToString,
                $"Request starting {_httpContext.Request.Protocol} {_httpContext.Request.Method} {_httpContext.Request.Scheme}://{_httpContext.Request.Host}{_httpContext.Request.PathBase}{_httpContext.Request.Path}{_httpContext.Request.QueryString} {_httpContext.Request.ContentType} {_httpContext.Request.ContentLength}",
                null);

            public IEnumerable<KeyValuePair<string, object>> GetValues() => _cachedGetValues ?? Interlocked.CompareExchange(
                ref _cachedGetValues,
                new[]
                {
                    new KeyValuePair<string, object>("Protocol", _httpContext.Request.Protocol),
                    new KeyValuePair<string, object>("Method", _httpContext.Request.Method),
                    new KeyValuePair<string, object>("ContentType", _httpContext.Request.ContentType),
                    new KeyValuePair<string, object>("ContentLength", _httpContext.Request.ContentLength),
                    new KeyValuePair<string, object>("Scheme", _httpContext.Request.Scheme.ToString()),
                    new KeyValuePair<string, object>("Host", _httpContext.Request.Host.ToString()),
                    new KeyValuePair<string, object>("PathBase", _httpContext.Request.PathBase.ToString()),
                    new KeyValuePair<string, object>("Path", _httpContext.Request.Path.ToString()),
                    new KeyValuePair<string, object>("QueryString", _httpContext.Request.QueryString.ToString()),
                },
                null);
        }

        private class HostingRequestFinished
        {
            internal static readonly Func<object, Exception, string> Callback = (state, exception) => ((HostingRequestFinished)state).ToString();

            private readonly HttpContext _httpContext;

            private IEnumerable<KeyValuePair<string, object>> _cachedGetValues;
            private string _cachedToString;

            public HostingRequestFinished(HttpContext httpContext)
            {
                _httpContext = httpContext;
            }

            public override string ToString() => _cachedToString ?? Interlocked.CompareExchange(
                ref _cachedToString,
                $"Request finished {_httpContext.Response.StatusCode} {_httpContext.Response.ContentType}",
                null);

            public IEnumerable<KeyValuePair<string, object>> GetValues() => _cachedGetValues ?? Interlocked.CompareExchange(
                ref _cachedGetValues,
                new[]
                {
                    new KeyValuePair<string, object>("StatusCode", _httpContext.Response.StatusCode),
                    new KeyValuePair<string, object>("ContentType", _httpContext.Response.ContentType),
                },
                null);
        }
    }
}

