using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace AssetBundleBrowser
{
    internal class MessageSystem
    {
        private static Texture2D s_ErrorIcon = null;
        private static Texture2D s_WarningIcon = null;
        private static Texture2D s_InfoIcon = null;
        private static Dictionary<MessageFlag, Message> s_MessageLookup = null;

        [Flags]
        internal enum MessageFlag
        {
            None = 0x0,

            Info = 0x80,                  //this flag is only used to check bits, not set.
            EmptyBundle = 0x81,
            EmptyFolder = 0x82,

            Warning = 0x8000,                  //this flag is only used to check bits, not set.
            WarningInChildren = 0x8100,
            AssetsDuplicatedInMultBundles = 0x8200,
            VariantBundleMismatch = 0x8400,

            Error = 0x800000,                  //this flag is only used to check bits, not set.
            ErrorInChildren = 0x810000,
            SceneBundleConflict = 0x820000,
            DependencySceneConflict = 0x840000,
        }

        internal class MessageState
        {
            //I have an enum and a set of enums to make some logic cleaner.  
            // The enum has masks for Error/Warning/Info that won't ever be in the set
            // this allows for easy checking of IsSet for error rather than specific errors. 
            private MessageFlag m_MessageFlags;
            private readonly HashSet<MessageFlag> m_MessageSet;


            internal MessageState()
            {
                m_MessageFlags = MessageFlag.None;
                m_MessageSet = new HashSet<MessageFlag>();
            }

            internal void Clear()
            {
                m_MessageFlags = MessageFlag.None;
                m_MessageSet.Clear();
            }

            internal void SetFlag(MessageFlag flag, bool on)
            {
                if (flag is MessageFlag.Info or MessageFlag.Warning or MessageFlag.Error)
                {
                    return;
                }

                if (on)
                {
                    m_MessageFlags |= flag;
                    _ = m_MessageSet.Add(flag);
                }
                else
                {
                    m_MessageFlags &= ~flag;
                    _ = m_MessageSet.Remove(flag);
                }
            }
            internal bool IsSet(MessageFlag flag)
            {
                return (m_MessageFlags & flag) == flag;
            }
            internal bool HasMessages()
            {
                return m_MessageFlags != MessageFlag.None;
            }

            internal MessageType HighestMessageLevel()
            {
                return IsSet(MessageFlag.Error)
                    ? MessageType.Error
                    : IsSet(MessageFlag.Warning) ? MessageType.Warning : IsSet(MessageFlag.Info) ? MessageType.Info : MessageType.None;
            }
            internal MessageFlag HighestMessageFlag()
            {
                MessageFlag high = MessageFlag.None;
                foreach (var f in m_MessageSet)
                {
                    if (f > high)
                    {
                        high = f;
                    }
                }
                return high;
            }

            internal List<Message> GetMessages()
            {
                var msgs = new List<Message>();
                foreach (var f in m_MessageSet)
                {
                    msgs.Add(GetMessage(f));
                }
                return msgs;
            }
        }
        internal static Texture2D GetIcon(MessageType sev)
        {
            return sev == MessageType.Error
                ? GetErrorIcon()
                : sev == MessageType.Warning ? GetWarningIcon() : sev == MessageType.Info ? GetInfoIcon() : null;
        }
        private static Texture2D GetErrorIcon()
        {
            if (s_ErrorIcon == null)
            {
                FindMessageIcons();
            }

            return s_ErrorIcon;
        }
        private static Texture2D GetWarningIcon()
        {
            if (s_WarningIcon == null)
            {
                FindMessageIcons();
            }

            return s_WarningIcon;
        }
        private static Texture2D GetInfoIcon()
        {
            if (s_InfoIcon == null)
            {
                FindMessageIcons();
            }

            return s_InfoIcon;
        }

        private static void FindMessageIcons()
        {
            s_ErrorIcon = EditorGUIUtility.FindTexture("console.errorIcon");
            s_WarningIcon = EditorGUIUtility.FindTexture("console.warnicon");
            s_InfoIcon = EditorGUIUtility.FindTexture("console.infoIcon");
        }
        internal class Message
        {
            internal Message(string msg, MessageType sev)
            {
                message = msg;
                severity = sev;
            }

            internal MessageType severity;
            internal string message;
            internal Texture2D Icon => GetIcon(severity);
        }

        internal static Message GetMessage(MessageFlag lookup)
        {
            if (s_MessageLookup == null)
            {
                InitMessages();
            }

            _ = s_MessageLookup.TryGetValue(lookup, out Message msg);
            msg ??= s_MessageLookup[MessageFlag.None];
            return msg;
        }

        private static void InitMessages()
        {
            s_MessageLookup = new Dictionary<MessageFlag, Message>
            {
                { MessageFlag.None, new Message(string.Empty, MessageType.None) },
                { MessageFlag.EmptyBundle, new Message("This bundle is empty.  Empty bundles cannot get saved with the scene and will disappear from this list if Unity restarts or if various other bundle rename or move events occur.", MessageType.Info) },
                { MessageFlag.EmptyFolder, new Message("This folder is either empty or contains only empty children.  Empty bundles cannot get saved with the scene and will disappear from this list if Unity restarts or if various other bundle rename or move events occur.", MessageType.Info) },
                { MessageFlag.WarningInChildren, new Message("Warning in child(ren)", MessageType.Warning) },
                { MessageFlag.AssetsDuplicatedInMultBundles, new Message("Assets being pulled into this bundle due to dependencies are also being pulled into another bundle.  This will cause duplication in memory", MessageType.Warning) },
                { MessageFlag.VariantBundleMismatch, new Message("Variants of a given bundle must have exactly the same assets between them based on a Name.Extension (without Path) comparison. These bundle variants fail that check.", MessageType.Warning) },
                { MessageFlag.ErrorInChildren, new Message("Error in child(ren)", MessageType.Error) },
                { MessageFlag.SceneBundleConflict, new Message("A bundle with one or more scenes must only contain scenes.  This bundle has scenes and non-scene assets.", MessageType.Error) },
                { MessageFlag.DependencySceneConflict, new Message("The folder added to this bundle has pulled in scenes and non-scene assets.  A bundle must only have one type or the other.", MessageType.Error) }
            };
        }
    }

}