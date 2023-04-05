# exit when any command fails
set -e

cfn-lint --non-zero-exit-code error templates/cfn-macros.template
