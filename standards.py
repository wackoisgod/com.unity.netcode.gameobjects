#!/usr/bin/env python3

import sys
import os
import argparse

parser = argparse.ArgumentParser()

parser.add_argument(
    "-i", "--install",
    help="install help text here (todo)",
    action="store_true")

parser.add_argument(
    "-c", "--check",
    help="check help text here (todo)",
    action="store_true")

parser.add_argument(
    "-f", "--fix",
    help="fix help text here (todo)",
    action="store_true")

if len(sys.argv) == 1:
    parser.print_help(sys.stderr)
    exit(1)

args = parser.parse_args()


if args.install:
    print("execute install")
    answer = input("install? [Y/n]\n").lower()
    if answer in ["yes", "y", "ye", ""]:
        print("install: yes")
    elif answer in ["no", "n"]:
        print("install: no")
    else:
        print("install: error")


if args.check:
    print("execute check")


if args.fix:
    print("execute fix")


print("pre-push hook executes standards.py")
exit(0)
