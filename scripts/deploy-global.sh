# exit when any command fails
set -e

# global resources deploy only in us-west-2!

aws cloudformation package --template-file templates/cfn-macros.template --s3-bucket ${BUCKET_NAME_PREFIX}-us-west-2 --s3-prefix ${BUCKET_KEY_PREFIX}/email-service/${CODEBUILD_RESOLVED_SOURCE_VERSION} --region us-west-2 > cfn-macros.template.published
aws cloudformation deploy --stack-name cfn-macros --template-file cfn-macros.template.published --no-fail-on-empty-changeset --capabilities CAPABILITY_IAM CAPABILITY_AUTO_EXPAND --region us-west-2
