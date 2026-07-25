#!/usr/bin/env python3
"""Convert NUnit test files to xUnit v3 syntax - v2 with parenthesis balancing."""
import re
import os

TEST_DIR = os.path.dirname(os.path.abspath(__file__))


def find_matching_paren(text, start):
    """Find the index of the matching closing paren, handling nesting."""
    depth = 1  # start inside the Assert.That( paren
    for i in range(start, len(text)):
        if text[i] == '(':
            depth += 1
        elif text[i] == ')':
            depth -= 1
            if depth == 0:
                return i
    return -1


def find_top_level_comma(text):
    """Find the first top-level comma (not inside parens/brackets/braces/strings)."""
    depth_paren = 0
    depth_bracket = 0
    depth_brace = 0
    in_string = False
    string_char = None
    
    for i, ch in enumerate(text):
        if in_string:
            if ch == '\\' and i + 1 < len(text):
                continue
            if ch == string_char:
                in_string = False
            continue
        if ch in ("'", '"'):
            in_string = True
            string_char = ch
            continue
        if ch == '(':
            depth_paren += 1
        elif ch == ')':
            depth_paren -= 1
        elif ch == '[':
            depth_bracket += 1
        elif ch == ']':
            depth_bracket -= 1
        elif ch == '{':
            depth_brace += 1
        elif ch == '}':
            depth_brace -= 1
        elif ch == ',' and depth_paren == 0 and depth_bracket == 0 and depth_brace == 0:
            return i
    return -1


def convert_assert_that(text):
    """Convert Assert.That(expr, constraint[, message]) to xUnit equivalent."""
    # Find the matching closing paren for Assert.That(
    idx = text.find('Assert.That(')
    if idx < 0:
        return text
    
    start = idx + len('Assert.That(')
    end = find_matching_paren(text, start)
    if end < 0:
        return text
    
    inner = text[start:end]
    
    # Find top-level comma separating expr from constraint
    comma = find_top_level_comma(inner)
    if comma < 0:
        return text
    
    expr = inner[:comma].strip()
    constraint_part = inner[comma + 1:].strip()
    
    # Check for optional message string
    msg = ''
    # Find the last top-level comma in constraint_part (not inside parens/brackets/strings)
    # to separate the constraint from an optional message argument
    paren_depth = 0
    bracket_depth = 0
    brace_depth = 0
    in_string = False
    last_top_comma = -1
    s = constraint_part
    for ci in range(len(s) - 1, -1, -1):
        ch = s[ci]
        if ch == '"' and (ci == 0 or s[ci - 1] != '\\\\'):
            in_string = not in_string
        elif ch == ')':
            paren_depth += 1
        elif ch == '(':
            paren_depth -= 1
        elif ch == ']':
            bracket_depth += 1
        elif ch == '[':
            bracket_depth -= 1
        elif ch == '}':
            brace_depth += 1
        elif ch == '{':
            brace_depth -= 1
        elif ch == ',' and paren_depth == 0 and bracket_depth == 0 and brace_depth == 0 and not in_string:
            last_top_comma = ci
            break
    
    if last_top_comma >= 0:
        after = constraint_part[last_top_comma + 1:].strip()
        # Message can be string literal, interpolated string, or variable
        msg = ', ' + after
        constraint_part = constraint_part[:last_top_comma].strip()
    
    # Remove trailing whitespace/newlines from expr
    expr = expr.replace('\n', ' ').replace('\r', '')
    expr = re.sub(r'\s+', ' ', expr)
    
    # Now convert based on constraint
    constraint = constraint_part.strip()
    
    result = None
    
    # Is.EqualTo(x) [possibly with .Within(p)]
    m = re.match(r'Is\.EqualTo\((.+)\)(?:\.Within\(([^)]+)\))?\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        precision = m.group(2)
        if precision:
            result = f'Assert.Equal({expected}, {expr}, {precision})'
        else:
            result = f'Assert.Equal({expected}, {expr})'
    
    # Is.Not.EqualTo(x)
    m = re.match(r'Is\.Not\.EqualTo\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.NotEqual({expected}, {expr})'
    
    # Is.Not.Null
    if re.match(r'Is\.Not\.Null\s*$', constraint, re.DOTALL):
        result = f'Assert.NotNull({expr}{msg})'
    
    # Is.Null
    if re.match(r'Is\.Null\s*$', constraint, re.DOTALL):
        result = f'Assert.Null({expr}{msg})'
    
    # Is.SameAs(x)
    m = re.match(r'Is\.SameAs\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.Same({expected}, {expr}{msg})'
    
    # Is.Not.SameAs(x)
    m = re.match(r'Is\.Not\.SameAs\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.NotSame({expected}, {expr})'
    
    # Is.True
    if re.match(r'Is\.True\s*$', constraint, re.DOTALL):
        result = f'Assert.True({expr}{msg})'
    
    # Is.False
    if re.match(r'Is\.False\s*$', constraint, re.DOTALL):
        result = f'Assert.False({expr}{msg})'
    
    # Is.TypeOf<T>()
    m = re.match(r'Is\.TypeOf<([^>]+)>\(\)\s*$', constraint, re.DOTALL)
    if m:
        type_name = m.group(1)
        result = f'Assert.IsType<{type_name}>({expr}{msg})'
    
    # Is.InstanceOf<T>()
    m = re.match(r'Is\.InstanceOf<([^>]+)>\(\)\s*$', constraint, re.DOTALL)
    if m:
        type_name = m.group(1)
        result = f'Assert.IsAssignableFrom<{type_name}>({expr})'
    
    # Is.Empty
    if re.match(r'Is\.Empty\s*$', constraint, re.DOTALL):
        result = f'Assert.Empty({expr})'
    
    # Is.Not.Empty
    if re.match(r'Is\.Not\.Empty\s*$', constraint, re.DOTALL):
        result = f'Assert.NotEmpty({expr})'
    
    # Does.Contain(x)
    m = re.match(r'Does\.Contain\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.Contains({expected}, {expr})'
    
    # Has.Length.EqualTo(n)
    m = re.match(r'Has\.Length\.EqualTo\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.Equal({expected}, {expr}.Length)'
    
    # Has.Count.EqualTo(n)
    m = re.match(r'Has\.Count\.EqualTo\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.Equal({expected}, {expr}.Count)'
    
    # Has.Count.GreaterThanOrEqualTo(n)
    m = re.match(r'Has\.Count\.GreaterThanOrEqualTo\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.True({expr}.Count >= {expected})'
    
    # Is.GreaterThan(x)
    m = re.match(r'Is\.GreaterThan\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.True({expr} > {expected})'
    
    # Is.LessThan(x)
    m = re.match(r'Is\.LessThan\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.True({expr} < {expected})'
    
    # Is.LessThanOrEqualTo(x)
    m = re.match(r'Is\.LessThanOrEqualTo\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.True({expr} <= {expected})'
    
    # Is.GreaterThanOrEqualTo(x)
    m = re.match(r'Is\.GreaterThanOrEqualTo\((.+)\)\s*$', constraint, re.DOTALL)
    if m:
        expected = m.group(1).strip()
        result = f'Assert.True({expr} >= {expected})'
    
    # Is.InRange(a, b)
    m = re.match(r'Is\.InRange\(([^,]+),\s*([^)]+)\)\s*$', constraint, re.DOTALL)
    if m:
        low = m.group(1).strip()
        high = m.group(2).strip()
        result = f'Assert.InRange({expr}, {low}, {high})'
    
    if result:
        return text[:idx] + result + text[end + 1:]
    
    # If we didn't match any constraint, leave as-is
    return text


def convert_file(path):
    with open(path) as f:
        text = f.read()
    original = text

    # 1. using statement
    text = text.replace('using NUnit.Framework;', 'using Xunit;')

    # 2. [TestFixture] (on its own line)
    text = re.sub(r'^[ \t]*\[TestFixture\]\s*\n', '', text, flags=re.MULTILINE)

    # 3. [Test] -> [Fact]
    text = text.replace('[Test]', '[Fact]')

    # 4. [TestCase(...)] -> [Theory] + [InlineData(...)]
    lines = text.split('\n')
    out = []
    pending_theory = False
    for line in lines:
        stripped = line.strip()
        if stripped.startswith('[TestCase('):
            if not pending_theory:
                out.append('    [Theory]')
                pending_theory = True
            out.append(line.replace('[TestCase(', '[InlineData(', 1))
        else:
            pending_theory = False
            out.append(line)
    text = '\n'.join(out)

    # 5. Assert.Pass(...) -> comment out
    text = re.sub(
        r'^([ \t]*)Assert\.Pass\((.*?)\);\s*',
        r'\1// Assert.Pass(\2);',
        text,
        flags=re.MULTILINE
    )

    # 6. Assert.That(...) - iterative conversion
    # Loop because after converting one, we need to check for more
    while 'Assert.That(' in text:
        new_text = convert_assert_that(text)
        if new_text == text:
            break  # No progress, avoid infinite loop
        text = new_text

    if text != original:
        with open(path, 'w') as f:
            f.write(text)
        return True
    return False


def main():
    files = sorted(f for f in os.listdir(TEST_DIR) if f.endswith('Tests.cs'))
    converted = 0
    for fname in files:
        path = os.path.join(TEST_DIR, fname)
        if convert_file(path):
            print(f"  Converted: {fname}")
            converted += 1
    print(f"\nTotal: {converted}/{len(files)} files modified")


if __name__ == '__main__':
    main()
