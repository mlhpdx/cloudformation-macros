# exit when any command fails
set -e -x

sam build --template-file templates/cfn-macros.template

# deploy to all of the regions enabled in the account
export ACCOUNT_REGIONS=$(aws account list-regions \
  --region-opt-status-contains "ENABLED" "ENABLED_BY_DEFAULT" \
  --no-paginate \
  --query "Regions[].RegionName" \
  --output text)

for region in ${DEPLOY_TO_REGIONS:-ACCOUNT_REGIONS}; do
  sam deploy \
  --s3-bucket ${BUCKET_NAME_PREFIX}-${region} \
  --s3-prefix ${BUCKET_KEY_PREFIX}/email-service/${CODEBUILD_RESOLVED_SOURCE_VERSION} \
  --stack-name cfn-macros \
  --no-fail-on-empty-changeset \
  --capabilities CAPABILITY_IAM CAPABILITY_AUTO_EXPAND \
  --region $region
done
