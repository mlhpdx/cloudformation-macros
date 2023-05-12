# exit when any command fails
set -e

sam build --template-file templates/cfn-macros.template

sam deploy --s3-bucket ${BUCKET_NAME_PREFIX}-us-west-2 --s3-prefix ${BUCKET_KEY_PREFIX}/email-service/${CODEBUILD_RESOLVED_SOURCE_VERSION} --stack-name cfn-macros --no-fail-on-empty-changeset --capabilities CAPABILITY_IAM CAPABILITY_AUTO_EXPAND --region us-west-2
sam deploy --s3-bucket ${BUCKET_NAME_PREFIX}-us-east-1 --s3-prefix ${BUCKET_KEY_PREFIX}/email-service/${CODEBUILD_RESOLVED_SOURCE_VERSION} --stack-name cfn-macros --no-fail-on-empty-changeset --capabilities CAPABILITY_IAM CAPABILITY_AUTO_EXPAND --region us-east-1
sam deploy --s3-bucket ${BUCKET_NAME_PREFIX}-eu-west-1 --s3-prefix ${BUCKET_KEY_PREFIX}/email-service/${CODEBUILD_RESOLVED_SOURCE_VERSION} --stack-name cfn-macros --no-fail-on-empty-changeset --capabilities CAPABILITY_IAM CAPABILITY_AUTO_EXPAND --region eu-west-1
