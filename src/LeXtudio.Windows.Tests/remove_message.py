#!/usr/bin/env python3
"""Remove message arguments from xUnit assertions that don't support them in v3."""
import os

def remove_message(line, func_name, min_args=1):
    '''Remove the last message argument from func_name(...) if it has more than min_args top-level arguments.'''
    idx = 0
    while True:
        idx = line.find(func_name + '(', idx)
        if idx < 0:
            break
        
        start = idx + len(func_name) + 1
        
        # Find matching close paren
        depth = 1
        in_string = False
        string_char = None
        close_pos = -1
        for i in range(start, len(line)):
            ch = line[i]
            if in_string:
                if ch == '\\' and i + 1 < len(line):
                    continue
                if ch == string_char:
                    in_string = False
                continue
            if ch in ("'", '"'):
                in_string = True
                string_char = ch
                continue
            if ch == '(':
                depth += 1
            elif ch == ')':
                depth -= 1
                if depth == 0:
                    close_pos = i
                    break
        
        if close_pos < 0:
            idx += 1
            continue
        
        # Count ALL top-level commas and track the last one
        depth = 0
        in_string = False
        string_char = None
        comma_count = 0
        last_comma = -1
        
        for i in range(start, close_pos):
            ch = line[i]
            if in_string:
                if ch == '\\' and i + 1 < close_pos:
                    continue
                if ch == string_char:
                    in_string = False
                continue
            if ch in ("'", '"'):
                in_string = True
                string_char = ch
                continue
            if ch == '(':
                depth += 1
            elif ch == ')':
                depth -= 1
            elif ch == ',' and depth == 0:
                comma_count += 1
                last_comma = i
        
        # Only remove last arg if we have more than min_args
        if last_comma >= 0 and comma_count >= min_args:
            line = line[:last_comma] + ')' + line[close_pos + 1:]
            idx = last_comma + 1
        else:
            idx += 1
    
    return line


test_dir = '/Users/lextm/uno-tools/WindowsShims/src/LeXtudio.Windows.Tests'
for fname in sorted(os.listdir(test_dir)):
    if not fname.endswith('Tests.cs'):
        continue
    path = os.path.join(test_dir, fname)
    with open(path) as f:
        lines = f.readlines()
    changed = False
    new_lines = []
    for line in lines:
        original = line.rstrip('\n\r')
        fixed = remove_message(original, 'Assert.NotNull', min_args=1)
        fixed = remove_message(fixed, 'Assert.Null', min_args=1)
        fixed = remove_message(fixed, 'Assert.True', min_args=1)
        fixed = remove_message(fixed, 'Assert.False', min_args=1)
        fixed = remove_message(fixed, 'Assert.Same', min_args=2)
        if fixed != original:
            changed = True
        new_lines.append(fixed)
    if changed:
        with open(path, 'w') as f:
            f.write('\n'.join(new_lines))
        print(f'  Fixed: {fname}')
print('Done')
