﻿using NostreetsExtensions.DataControl.Enums;
using System;

namespace NostreetsExtensions.DataControl.Classes
{
    public class Token : DBObject<string>
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public DateTime ExpirationDate { get; set; }

        public TokenType Type { get; set; }

        public bool IsValidated { get; set; }
    }

    public class TokenRequest
    {
        public string TokenId { get; set; }

        public string UserId { get; set; }

        public string Code { get; set; }
    }
}