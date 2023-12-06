using System;
using System.Collections.Generic;
using System.Reflection;

namespace Event
{
    /// <summary>
    /// 本模块主要目的是兜底一部分delegate或者事件的“自动释放”，防止出现过多的事件忘记释放导致的回调报错
    /// </summary>
    public static class EventMgr
    {
        static EventMgr()
        {
            Init();
        }

        public static void Init()
        {
            if (bInit) return;

            bInit = true;
            mEventHosts = new Dictionary<object, HashSet<Proxy>>();
        }

        public static void DeInit()
        {
            // 故意置空好暴露出不正当时机注册的回调
            mEventHosts = null;
            bInit = false;
        }

        public static void AttachListener(ref Action broadcastor, Action listener, object host)
        {
            broadcastor = CacheListener(broadcastor, listener, host, out var proxy) as Action;
            broadcastor += proxy.Excute;
        }

        public static void AttachListener<T>(ref Action<T> broadcastor, Action<T> listener, object host)
        {
            broadcastor = CacheListener(broadcastor, listener, host, out var proxy) as Action<T>;
            broadcastor += proxy.Excute;
        }

        public static void AttachListener<T1, T2>(ref Action<T1, T2> broadcastor, Action<T1, T2> listener, object host)
        {
            broadcastor = CacheListener(broadcastor, listener, host, out var proxy) as Action<T1, T2>;
            broadcastor += proxy.Excute;
        }

        public static void AttachListener<T1, T2, T3>(ref Action<T1, T2, T3> broadcastor, Action<T1, T2, T3> listener,
            object host)
        {
            broadcastor = CacheListener(broadcastor, listener, host, out var proxy) as Action<T1, T2, T3>;
            broadcastor += proxy.Excute;
        }

        public static void AttachListener<T1, T2, T3, T4>(ref Action<T1, T2, T3, T4> broadcastor,
            Action<T1, T2, T3, T4> listener, object host)
        {
            broadcastor = CacheListener(broadcastor, listener, host, out var proxy) as Action<T1, T2, T3, T4>;
            broadcastor += proxy.Excute;
        }

        /// <summary>
        /// 注销Host对应的所有事件
        /// </summary>
        public static void DetachListener(object host)
        {
            if (!bInit) return;

            if (mEventHosts.TryGetValue(host, out var listenerList))
            {
                listenerList.Clear();
            }

            mEventHosts.Remove(host);
        }

        private static Delegate CacheListener(Delegate broadcaster, Delegate listener, object host, out Proxy proxy)
        {
            if (!mEventHosts.TryGetValue(host, out var set))
            {
                set = new HashSet<Proxy>();
                mEventHosts.Add(host, set);
            }

            // 只能延迟到下次注册的时候，清理掉老的绑定的回调，当然老的回调不会被调用
            broadcaster = CleanProcess(broadcaster, listener, host, set) as Action;
            proxy = new Proxy() { host = host, listener = listener };
            set.Add(proxy);

            return broadcaster;
        }

        /// <summary>
        /// 注册的时候清理下老的没用的绑定的回调
        /// </summary>
        /// <param name="broadcaster"></param>
        /// <param name="listener"></param>
        /// <param name="host"></param>
        /// <param name="allProxies"></param>
        /// <returns></returns>
        private static Delegate CleanProcess(Delegate broadcaster, Delegate listener, object host,
            HashSet<Proxy> allProxies)
        {
            if (broadcaster != null)
            {
                var broadcasterList = broadcaster.GetInvocationList();
                foreach (var invoker in broadcasterList)
                {
                    if (invoker.Target is Proxy proxy)
                    {
                        // 同一个host的同一个method，只允许存在一个绑定信息
                        bool bRemove = proxy.host == host && proxy.listener.Method == listener.Method;
                        // 清理掉老的在其他地方无法清理掉的已经绑定的回调
                        bRemove |= DidProxyInvalid(proxy.host, proxy);

                        if (bRemove)
                        {
                            allProxies.Remove(proxy);
                            broadcaster = Delegate.Remove(broadcaster, invoker);
                        }
                    }
                }
            }

            return broadcaster;
        }

        private static bool DidProxyInvalid(object host, Proxy proxy)
        {
            return mEventHosts == null || !mEventHosts.TryGetValue(host, out var proxies) || !proxies.Contains(proxy);
        }

        private struct Proxy
        {
            public object host;
            public Delegate listener;

            public void Excute()
            {
                if (DidProxyInvalid(host, this))
                {
                    return;
                }

                if (listener is Action action)
                {
                    action();
                }
            }

            public void Excute<T>(T arg)
            {
                if (DidProxyInvalid(host, this))
                {
                    return;
                }

                if (listener is Action<T> action)
                {
                    action(arg);
                }
            }

            public void Excute<T1, T2>(T1 arg1, T2 arg2)
            {
                if (DidProxyInvalid(host, this))
                {
                    return;
                }

                if (listener is Action<T1, T2> action)
                {
                    action(arg1, arg2);
                }
            }

            public void Excute<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
            {
                if (DidProxyInvalid(host, this))
                {
                    return;
                }

                if (listener is Action<T1, T2, T3> action)
                {
                    action(arg1, arg2, arg3);
                }
            }

            public void Excute<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
            {
                if (DidProxyInvalid(host, this))
                {
                    return;
                }

                if (listener is Action<T1, T2, T3, T4> action)
                {
                    action(arg1, arg2, arg3, arg4);
                }
            }
        }

        static bool bInit;
        static Dictionary<object, HashSet<Proxy>> mEventHosts;
    }
}